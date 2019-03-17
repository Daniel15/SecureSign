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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Libgpgme;
using SecureSign.Core;

namespace SecureSign.Tools
{
	/// <summary>
	/// Handles adding new keys
	/// </summary>
	public class AddKey
	{
		private readonly ISecretStorage _secretStorage;
		private readonly IPasswordGenerator _passwordGenerator;
		private readonly Context _gpgContext;

		public AddKey(
			ISecretStorage secretStorage, 
			IPasswordGenerator passwordGenerator, 
			Context gpgContext
		)
		{
			_secretStorage = secretStorage;
			_passwordGenerator = passwordGenerator;
			_gpgContext = gpgContext;
		}

		public void Run(string inputPath)
		{
			// Ensure input file exists
			if (!File.Exists(inputPath))
			{
				throw new Exception("File does not exist: " + inputPath);
			}

			Add(inputPath);
			Console.WriteLine();
			Console.WriteLine("This secret code is required whenever you create an access token that uses this key.");
			Console.WriteLine("Store this secret code in a SECURE PLACE! The code is not stored anywhere, ");
			Console.WriteLine("so if you lose it, you will need to re-install the key.");
		}

		private void Add(string inputPath)
		{
			var fileName = Path.GetFileName(inputPath);
			switch (Path.GetExtension(fileName))
			{
				case ".pfx":
					AddAuthenticode(inputPath);
					break;

				case ".gpg":
					AddGpg(inputPath);
					break;

				default:
					throw new Exception(
						"Unrecognised file extension. Please use .pfx for Authenticode or .gpg for GPG."
					);
			}
		}

		private void AddAuthenticode(string inputPath)
		{
			// Ensure output file does not exist
			var fileName = Path.GetFileName(inputPath);
			ThrowIfSecretExists(fileName);

			var password = ConsoleUtils.PasswordPrompt("Password");
			var cert = new X509Certificate2(File.ReadAllBytes(inputPath), password, X509KeyStorageFlags.Exportable);

			var code = _passwordGenerator.Generate();
			_secretStorage.SaveSecret(fileName, cert, code);
			Console.WriteLine();
			Console.WriteLine($"Saved {fileName} ({cert.FriendlyName})");
			Console.WriteLine($"Subject: {cert.SubjectName.Format(false)}");
			Console.WriteLine($"Issuer: {cert.IssuerName.Format(false)}");
			Console.WriteLine($"Valid from {cert.NotBefore} until {cert.NotAfter}");
			Console.WriteLine();
			Console.WriteLine($"Secret Code: {code}");
		}

		private void AddGpg(string inputPath)
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

		private static List<Key> GetAndVerifyKeys(IEnumerable<string> fingerprints, Context ctx)
		{
			// Ensure all keys work by signing a message with them
			var original = new GpgmeMemoryData {FileName = "original.txt"};
			var writer = new BinaryWriter(original, Encoding.UTF8);
			writer.Write("Hello World!");

			var keys = new List<Key>();

			foreach (var fingerprint in fingerprints)
			{
				var output = new GpgmeMemoryData {FileName = "original.txt.gpg"};
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
						_gpgContext.KeyStore.Import(tempFileData);
					}
				}
				finally
				{
					File.Delete(tempFile);
				}
			}
		}

		private void EnsureGpgKeysDoNotExist(List<Key> keys)
		{
			foreach (var key in keys)
			{
				foreach (var subkey in key.Subkeys)
				{
					ThrowIfSecretExists(subkey.Keygrip + ".key");
				}
			}
		}

		private void ThrowIfSecretExists(string fileName)
		{
			var outputPath = _secretStorage.GetPathForSecret(fileName);
			if (File.Exists(outputPath))
			{
				throw new Exception(outputPath + " already exists! I'm not going to overwrite it.");
			}
		}
	}
}
