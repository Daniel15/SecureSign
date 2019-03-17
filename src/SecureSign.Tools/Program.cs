/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SecureSign.Core;
using SecureSign.Core.Extensions;
using SecureSign.Core.Models;

namespace SecureSign.Tools
{
	public class Program
	{
		static int Main(string[] args)
		{
			var config = BuildConfig();
			var services = new ServiceCollection();
			services.AddSecureSignCore(config);

			var provider = services.BuildServiceProvider();
			var program = ActivatorUtilities.CreateInstance<Program>(provider);
			return program.Run(args);
		}

		private static IConfiguration BuildConfig()
		{
			return new ConfigurationBuilder()
				.AddSecureSignConfig()
				.Build();
		}

		private readonly ISecretStorage _secretStorage;
		private readonly IAccessTokenSerializer _accessTokenSerializer;
		private readonly IServiceProvider _provider;
		private readonly PathConfig _pathConfig;

		public Program(ISecretStorage secretStorage, IAccessTokenSerializer accessTokenSerializer, IOptions<PathConfig> pathConfig, IServiceProvider provider)
		{
			_secretStorage = secretStorage;
			_accessTokenSerializer = accessTokenSerializer;
			_provider = provider;
			_pathConfig = pathConfig.Value;
		}

		private int Run(string[] args)
		{
			var app = new CommandLineApplication();
			app.Name = "SecureSignTools";
			app.HelpOption("-?|-h|--help");
			app.OnExecute(() => app.ShowHelp());

			app.Command("addkey", command =>
			{
				command.Description = "Add a new key";
				var pathArg = command.Argument("path", "Path to the key file to add");
				command.OnExecute(() =>
				{
					var inputPath = pathArg.Value;
					if (string.IsNullOrWhiteSpace(inputPath))
					{
						Console.WriteLine("Please include the file name to add");
						return 1;
					}
					ActivatorUtilities.CreateInstance<AddKey>(_provider).Run(inputPath);
					Console.ReadKey();
					return 0;
				});
			});

			app.Command("addtoken", command =>
			{
				command.Description = "Add a new access token";
				command.OnExecute(() =>
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

					// If we got here, the key is valid
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

					Console.WriteLine();
					Console.WriteLine("Created new access token:");
					Console.WriteLine(encodedAccessToken);
					return 0;
				});
			});

			try
			{
				return app.Execute(args);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("ERROR: " + ex.Message);
#if DEBUG
				throw;
#else
				return 1;
#endif
			}
		}
	}
}
