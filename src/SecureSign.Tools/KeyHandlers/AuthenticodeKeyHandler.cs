/**
 * Copyright (c) 2019 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using SecureSign.Core;
using SecureSign.Core.Extensions;
using SecureSign.Core.Models;

namespace SecureSign.Tools.KeyHandlers
{
	/// <summary>
	/// Handles storing and creating access tokens for Authenticode keys
	/// </summary>
	class AuthenticodeKeyHandler : IKeyHandler
	{
		private readonly IPasswordGenerator _passwordGenerator;
		private readonly ISecretStorage _secretStorage;

		public AuthenticodeKeyHandler(IPasswordGenerator passwordGenerator, ISecretStorage secretStorage)
		{
			_passwordGenerator = passwordGenerator;
			_secretStorage = secretStorage;
		}

		/// <summary>
		/// Gets the key type that this key handler supports
		/// </summary>
		public KeyType KeyType => KeyType.Authenticode;

		/// <summary>
		/// Adds a new key to the secret storage.
		/// </summary>
		/// <param name="inputPath"></param>
		public void AddKey(string inputPath)
		{
			// Ensure output file does not exist
			var fileName = Path.GetFileName(inputPath);
			_secretStorage.ThrowIfSecretExists(fileName);

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
			var comment = ConsoleUtils.Prompt("Comment (optional)");

			Console.WriteLine();
			Console.WriteLine("Signing settings:");
			var desc = ConsoleUtils.Prompt("Description");
			var url = ConsoleUtils.Prompt("Product/Application URL");

			var accessToken = new AccessToken
			{
				Id = Guid.NewGuid().ToShortGuid(),
				Code = code,
				IssuedAt = DateTime.Now,
				KeyName = name,
			};
			var accessTokenConfig = new AccessTokenConfig
			{
				Comment = comment,
				IssuedAt = accessToken.IssuedAt,
				Valid = true,

				SignDescription = desc,
				SignUrl = url,
			};
			return (accessToken, accessTokenConfig);
		}
	}
}
