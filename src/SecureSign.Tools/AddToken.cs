/**
 * Copyright (c) 2019 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using Libgpgme;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SecureSign.Core;
using SecureSign.Core.Extensions;
using SecureSign.Core.Models;

namespace SecureSign.Tools
{
	/// <summary>
	/// Handles creating new access tokens
	/// </summary>
	class AddToken
	{
		private readonly ISecretStorage _secretStorage;
		private readonly IAccessTokenSerializer _accessTokenSerializer;
		private readonly Context _gpgContext;
		private readonly PathConfig _pathConfig;

		public AddToken(
			ISecretStorage secretStorage, 
			IAccessTokenSerializer accessTokenSerializer, 
			IOptions<PathConfig> pathConfig,
			Context gpgContext
		)
		{
			_secretStorage = secretStorage;
			_accessTokenSerializer = accessTokenSerializer;
			_gpgContext = gpgContext;
			_pathConfig = pathConfig.Value;
		}

		public int Run()
		{
			var name = ConsoleUtils.Prompt("Key name");
			var code = ConsoleUtils.Prompt("Secret code");

			try
			{
				_secretStorage.LoadSecret(name, code);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Could not load key: {ex.Message}");
				Console.Error.WriteLine("Please check that the name and secret code are valid.");
				return 1;
			}

			AccessToken accessToken;
			AccessTokenConfig accessTokenConfig;
			switch (Path.GetExtension(name))
			{
				case ".pfx":
					(accessToken, accessTokenConfig) = CreateTokenForAuthenticode(code, name);
					break;

				case ".gpg":
					(accessToken, accessTokenConfig) = CreateTokenForGpg(code, name);
					break;

				default:
					throw new Exception("Unrecognised file extension.");
			}



			var encodedAccessToken = SaveAccessToken(accessToken, accessTokenConfig);
			Console.WriteLine();
			Console.WriteLine("Created new access token:");
			Console.WriteLine(encodedAccessToken);
			return 0;
		}

		/// <summary>
		/// Creates an access token for an Authenticode signing key
		/// </summary>
		/// <param name="code">Decryption code for key</param>
		/// <param name="name">Name of the key</param>
		/// <returns>Access token and its config</returns>
		private (AccessToken accessToken, AccessTokenConfig accessTokenConfig) CreateTokenForAuthenticode(
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

		/// <summary>
		/// Creates an access token for a GPG signing key
		/// </summary>
		/// <param name="code">Decryption code for key</param>
		/// <param name="name">Name of the key</param>
		/// <returns>Access token and its config</returns>
		private (AccessToken accessToken, AccessTokenConfig accessTokenConfig) CreateTokenForGpg(
			string code,
			string name
		)
		{
			var fingerprint = ConsoleUtils.Prompt("Key ID");
			// Validate keyId is legit
			var key = _gpgContext.KeyStore.GetKey(fingerprint, secretOnly: false);
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

		/// <summary>
		/// Saves the provided access token to the config
		/// </summary>
		/// <param name="accessToken"></param>
		/// <param name="accessTokenConfig">Config to save</param>
		/// <returns>Serialized access token for use in SecureSign requests</returns>
		private string SaveAccessToken(AccessToken accessToken, AccessTokenConfig accessTokenConfig)
		{
			// If this is the first time an access token is being added, we need to create the config file
			if (!File.Exists(_pathConfig.AccessTokenConfig))
			{
				File.WriteAllText(_pathConfig.AccessTokenConfig, JsonConvert.SerializeObject(new
				{
					AccessTokens = new Dictionary<string, AccessToken>()
				}));
			}

			// Save access token config to config file
			dynamic configFile = JObject.Parse(File.ReadAllText(_pathConfig.AccessTokenConfig));
			configFile.AccessTokens[accessToken.Id] = JToken.FromObject(accessTokenConfig);
			File.WriteAllText(_pathConfig.AccessTokenConfig, JsonConvert.SerializeObject(configFile, Formatting.Indented));

			var encodedAccessToken = _accessTokenSerializer.Serialize(accessToken);
			return encodedAccessToken;
		}
	}
}
