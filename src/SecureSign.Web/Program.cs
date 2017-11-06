/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SecureSign.Core.Extensions;

namespace SecureSign.Web
{
	public class Program
	{
		public static void Main(string[] args)
		{
			BuildWebHost(args).Run();
		}

		public static IWebHost BuildWebHost(string[] args) =>
			WebHost.CreateDefaultBuilder(args)
				.ConfigureAppConfiguration(builder => builder.AddSecureSignConfig())
				.ConfigureLogging(builder => builder.AddFile(
					pathFormat: "logs/{Date}.txt",
					levelOverrides: new Dictionary<string, LogLevel>
					{
						{"Microsoft", LogLevel.Warning}
					}
				))
				.UseStartup<Startup>()
				.UseKestrel(options =>
				{
					var rawIP = Environment.GetEnvironmentVariable("LISTEN_IP");
					if (!string.IsNullOrWhiteSpace(rawIP))
					{
						var ip = rawIP == "*" ? IPAddress.Any : IPAddress.Parse(rawIP);
						// Check if HTTP port was provided
						var httpPort = Environment.GetEnvironmentVariable("HTTP_PORT");
						if (!string.IsNullOrWhiteSpace(httpPort))
						{
							options.Listen(ip, int.Parse(httpPort));
						}

						// Check if HTTPS config was provided
						var httpsPort = Environment.GetEnvironmentVariable("HTTPS_PORT");
						var certFile = Environment.GetEnvironmentVariable("HTTPS_CERT");
						var certPassword = Environment.GetEnvironmentVariable("HTTPS_CERT_PASSWORD");
						if (
							!string.IsNullOrWhiteSpace(httpsPort) &&
							!string.IsNullOrWhiteSpace(certFile) &&
							!string.IsNullOrWhiteSpace(certPassword)
						)
						{
							options.Listen(ip, int.Parse(httpsPort), listenOptions =>
							{
								listenOptions.UseHttps(certFile, certPassword);
							});
						}
					}
				})
				.Build();
	}
}
