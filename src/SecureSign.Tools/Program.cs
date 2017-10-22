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
			return program.Run(args, config);
		}

		private static IConfiguration BuildConfig()
		{
			return new ConfigurationBuilder()
				.AddSecureSignConfig()
				.Build();
		}

		private readonly ISecretStorage _secretStorage;
		private readonly IAccessTokenSerializer _accessTokenSerializer;
		private readonly IPasswordGenerator _passwordGenerator;
		private IConfiguration _config;

		public Program(ISecretStorage secretStorage, IAccessTokenSerializer accessTokenSerializer, IPasswordGenerator passwordGenerator)
		{
			_secretStorage = secretStorage;
			_accessTokenSerializer = accessTokenSerializer;
			_passwordGenerator = passwordGenerator;
		}

		private int Run(string[] args, IConfiguration config)
		{
			_config = config;

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

					// Ensure input file exists
					if (!File.Exists(inputPath))
					{
						throw new Exception("File does not exist: " + inputPath);
					}

					// Ensure output file does not exist
					var fileName = Path.GetFileName(inputPath);
					var outputPath = _secretStorage.GetPathForSecret(fileName);
					if (File.Exists(outputPath))
					{
						throw new Exception(outputPath + " already exists! I'm not going to overwrite it.");
					}

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
					Console.WriteLine();
					Console.WriteLine("This secret code is required whenever you create an access token that uses this key.");
					Console.WriteLine("Store this secret code in a SECURE PLACE! The code is not stored anywhere, ");
					Console.WriteLine("so if you lose it, you will need to re-install the key.");
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

					// Save access token config to config file
					var configPath = _config["SecureSignWebConfigPath"];
					dynamic configFile = JObject.Parse(File.ReadAllText(configPath));
					configFile.AccessTokens[accessToken.Id] = JToken.FromObject(accessTokenConfig);
					File.WriteAllText(configPath, JsonConvert.SerializeObject(configFile, Formatting.Indented));

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
