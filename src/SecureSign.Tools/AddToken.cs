/**
 * Copyright (c) 2019 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SecureSign.Core;
using SecureSign.Core.Models;
using SecureSign.Tools.KeyHandlers;

namespace SecureSign.Tools
{
	/// <summary>
	/// Handles creating new access tokens
	/// </summary>
	class AddToken
	{
		private readonly ISecretStorage _secretStorage;
		private readonly IAccessTokenSerializer _accessTokenSerializer;
		private readonly IKeyHandlerFactory _keyHandlerFactory;
		private readonly PathConfig _pathConfig;

		public AddToken(
			ISecretStorage secretStorage, 
			IAccessTokenSerializer accessTokenSerializer, 
			IOptions<PathConfig> pathConfig,
			IKeyHandlerFactory keyHandlerFactory
		)
		{
			_secretStorage = secretStorage;
			_accessTokenSerializer = accessTokenSerializer;
			_keyHandlerFactory = keyHandlerFactory;
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

			var handler = _keyHandlerFactory.GetHandler(name);
			var (accessToken, accessTokenConfig) = handler.CreateAccessToken(code, name);
			var encodedAccessToken = SaveAccessToken(accessToken, accessTokenConfig);
			Console.WriteLine();
			Console.WriteLine("Created new access token:");
			Console.WriteLine(encodedAccessToken);
			return 0;
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
