/**
 * Copyright (c) 2019 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Libgpgme;
using SecureSign.Core;
using SecureSign.Core.Extensions;
using SecureSign.Core.Models;

namespace SecureSign.Tools.KeyHandlers
{
	/// <summary>
	/// Handles storing and creating access tokens for GPG keys
	/// </summary>
	class GpgKeyHandler : IKeyHandler
	{
		private readonly ISecretStorage _secretStorage;
		private readonly IPasswordGenerator _passwordGenerator;
		private readonly Context _ctx;

		public GpgKeyHandler(ISecretStorage secretStorage, IPasswordGenerator passwordGenerator, Context ctx)
		{
			_secretStorage = secretStorage;
			_passwordGenerator = passwordGenerator;
			_ctx = ctx;
		}

		/// <summary>
		/// Gets the file extension this key handler supports
		/// </summary>
		public string FileExtension => ".gpg";

		/// <summary>
		/// Adds a new key to the secret storage.
		/// </summary>
		/// <param name="inputPath"></param>
		public void AddKey(string inputPath)
		{
			// Create a temporary directory to hold the GPG key while we verify that it's legit
			var tempHomedir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Console.WriteLine($"Using {tempHomedir} as temp directory.");
			Directory.CreateDirectory(tempHomedir);
			try
			{
				using (var tempCtx = new Context())
				{
					tempCtx.SetEngineInfo(Protocol.OpenPGP, null, tempHomedir);
					tempCtx.PinentryMode = PinentryMode.Error;

					ImportResult result;
					using (var keyfile = new GpgmeFileData(inputPath))
					{
						result = tempCtx.KeyStore.Import(keyfile);
					}

					if (result == null || result.Imported == 0 || result.SecretImported == 0 || result.Imports == null)
					{
						throw new Exception("No secret keys found!");
					}

					var fingerprints = result.Imports.Where(x => x.NewSecretKey).Select(x => x.Fpr);
					Console.WriteLine($"Found fingerprints: {string.Join(", ", fingerprints)}");

					var keys = GetAndVerifyKeys(fingerprints, tempCtx);
					EnsureGpgKeysDoNotExist(keys);

					Console.WriteLine();
					ImportAndEncryptSecretGpgKeys(keys, tempHomedir);
					ImportPublicGpgKeys(keys, tempCtx);
				}
			}
			finally
			{
				Directory.Delete(tempHomedir, true);
			}
		}

		/// <summary>
		/// Gets all the keys with the specified fingerprints, and verifies that they can be used for signing
		/// </summary>
		/// <param name="fingerprints">Fingerprints of the keys</param>
		/// <param name="ctx">GPGME context to use</param>
		/// <returns>List of all the keys</returns>
		private static List<Key> GetAndVerifyKeys(IEnumerable<string> fingerprints, Context ctx)
		{
			// Ensure all keys work by signing a message with them
			var original = new GpgmeMemoryData { FileName = "original.txt" };
			var writer = new BinaryWriter(original, Encoding.UTF8);
			writer.Write("Hello World!");

			var keys = new List<Key>();

			foreach (var fingerprint in fingerprints)
			{
				var output = new GpgmeMemoryData { FileName = "original.txt.gpg" };
				var key = ctx.KeyStore.GetKey(fingerprint, true);
				ctx.Signers.Clear();
				ctx.Signers.Add(key);
				try
				{
					var signResult = ctx.Sign(original, output, SignatureMode.Detach);
					if (signResult.InvalidSigners != null)
					{
						var firstInvalidKey = signResult.InvalidSigners.First();
						throw new Exception(
							$"Invalid signer: {firstInvalidKey.Fingerprint} ({firstInvalidKey.Reason})"
						);
					}

					keys.Add(key);
				}
				catch (GpgmeException ex)
				{
					// TODO: Expose GPG error code through gpgme-sharp, to avoid string comparison
					if (ex.Message.Contains("NO_PIN_ENTRY"))
					{
						throw new Exception(
							"SecureSign does not support passphrase-protected GPG keys. " +
							"Please export your key without a passphrase."
						);
					}

					throw;
				}
			}

			return keys;
		}

		/// <summary>
		/// Imports the specified keys from the temporary storage, and encrypts them.
		/// Outputs details to the console.
		/// </summary>
		/// <param name="keys">Keys to import</param>
		/// <param name="tempHomedir">Temporary GPG homedir</param>
		private void ImportAndEncryptSecretGpgKeys(IEnumerable<Key> keys, string tempHomedir)
		{
			foreach (var key in keys)
			{
				foreach (var subkey in key.Subkeys)
				{
					var inputFilename = subkey.Keygrip + ".key";
					var outputFilename = subkey.Keygrip + ".gpg";
					var keygripPath = Path.Join(
						tempHomedir,
						"private-keys-v1.d",
						inputFilename
					);
					var code = _passwordGenerator.Generate();
					_secretStorage.SaveSecret(outputFilename, File.ReadAllBytes(keygripPath), code);
					File.Delete(keygripPath);

					Console.WriteLine($"- {subkey.KeyId}");
					Console.WriteLine($"  Expires: {subkey.Expires}");
					Console.WriteLine($"  Key name: {outputFilename}");
					Console.WriteLine($"  Secret Code: {code}");
					Console.WriteLine();
				}
			}
		}

		/// <summary>
		/// Imports the public GPG keys into the local GPG home directory
		/// </summary>
		/// <param name="keys">Keys to import</param>
		/// <param name="tempCtx">Temporary GPGME context</param>
		private void ImportPublicGpgKeys(IEnumerable<Key> keys, Context tempCtx)
		{
			foreach (var key in keys)
			{
				var tempFile = Path.GetTempFileName();
				try
				{
					// Export public key from temporary keychain and import into real keychain
					tempCtx.KeyStore.Export(key.KeyId, tempFile);
					using (var tempFileData = new GpgmeFileData(tempFile))
					{
						_ctx.KeyStore.Import(tempFileData);
					}
				}
				finally
				{
					File.Delete(tempFile);
				}
			}
		}

		/// <summary>
		/// Throws an exception if any of the specified keys already exist in the secret storage
		/// </summary>
		/// <param name="keys">Keys to check</param>
		private void EnsureGpgKeysDoNotExist(List<Key> keys)
		{
			foreach (var key in keys)
			{
				foreach (var subkey in key.Subkeys)
				{
					_secretStorage.ThrowIfSecretExists(subkey.Keygrip + ".gpg");
				}
			}
		}

		/// <summary>
		/// Creates a new access token to use the specified key
		/// </summary>
		/// <param name="code">Encryption code for the key</param>
		/// <param name="name">Name of the key</param>
		/// <returns>Access token and its config</returns>
		public (AccessToken accessToken, AccessTokenConfig accessTokenConfig) CreateAccessToken(
			string code, 
			string name
		)
		{
			var fingerprint = ConsoleUtils.Prompt("Key ID");
			// Validate keyId is legit
			var key = _ctx.KeyStore.GetKey(fingerprint, secretOnly: false);
			if (key == null)
			{
				throw new Exception($"Invalid key ID: {fingerprint}");
			}

			var comment = ConsoleUtils.Prompt("Comment (optional)");

			var accessToken = new AccessToken
			{
				Id = Guid.NewGuid().ToShortGuid(),
				Code = code,
				IssuedAt = DateTime.Now,
				KeyFingerprint = fingerprint,
				KeyName = name,
			};
			var accessTokenConfig = new AccessTokenConfig
			{
				Comment = comment,
				IssuedAt = accessToken.IssuedAt,
				Valid = true,
			};
			return (accessToken, accessTokenConfig);
		}
	}
}
