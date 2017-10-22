/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using SecureSign.Core.Models;

namespace SecureSign.Core.Extensions
{
	/// <summary>
	/// Extension methods for <see cref="IConfigurationBuilder"/>
	/// </summary>
	public static class ConfigurationBuilderExtensions
    {
		/// <summary>
		/// Adds SecureSign configuration information to the config builder.
		/// </summary>
		/// <param name="config"></param>
	    public static IConfigurationBuilder AddSecureSignConfig(this IConfigurationBuilder config)
		{
			var rootPath = BootstrapUtils.DetermineRootDirectory();

			config.Sources.Clear();
			return config
				.AddInMemoryCollection(new Dictionary<string, string>
				{
					// Default path configuration
					{"Paths:Root", rootPath},
					{"Paths:EncryptionKeys", Path.Combine(rootPath, "keys")},
					{"Paths:Certificates", Path.Combine(rootPath, "secrets")}, // https://youtu.be/_j-Tji1DueU
				})

				.SetBasePath(rootPath)
				// TODO: This should probably load `appsettings.{environment}.json too.
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile(PathConfig.ACCESS_TOKEN_FILENAME, optional: true, reloadOnChange: true)

				.AddEnvironmentVariables();
		}

	}
}
