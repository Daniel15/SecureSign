/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecureSign.Core.Extensions;
using SecureSign.Tools.KeyHandlers;

namespace SecureSign.Tools
{
	public class Program
	{
		static int Main(string[] args)
		{
			var config = BuildConfig();
			var services = new ServiceCollection();
			services.AddSecureSignCore(config);
			services.AddScoped<IKeyHandler, AuthenticodeKeyHandler>();
			services.AddScoped<IKeyHandler, GpgKeyHandler>();
			services.AddScoped<IKeyHandlerFactory, KeyHandlerFactory>();

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

		private readonly IKeyHandlerFactory _keyHandlerFactory;
		private readonly IServiceProvider _provider;

		public Program(IKeyHandlerFactory keyHandlerFactory, IServiceProvider provider)
		{
			_keyHandlerFactory = keyHandlerFactory;
			_provider = provider;
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

					if (!File.Exists(inputPath))
					{
						throw new Exception("File does not exist: " + inputPath);
					}

					var handler = _keyHandlerFactory.GetHandler(inputPath);
					handler.AddKey(inputPath);

					Console.WriteLine();
					Console.WriteLine("This secret code is required whenever you create an access token that uses this key.");
					Console.WriteLine("Store this secret code in a SECURE PLACE! The code is not stored anywhere, ");
					Console.WriteLine("so if you lose it, you will need to re-install the key.");
					Console.ReadKey();
					return 0;
				});
			});

			app.Command("addtoken", command =>
			{
				command.Description = "Add a new access token";
				command.OnExecute(() => ActivatorUtilities.CreateInstance<AddToken>(_provider).Run());
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
