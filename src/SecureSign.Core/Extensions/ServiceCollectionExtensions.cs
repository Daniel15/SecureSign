/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecureSign.Core.Models;
using SecureSign.Core.Signers;

namespace SecureSign.Core.Extensions
{
	/// <summary>
	/// Adds SecureSign services to the dependency injection container
	/// </summary>
    public static class ServiceCollectionExtensions
    {
	    /// <summary>
	    /// Adds SecureSign services to the dependency injection container
	    /// </summary>
		public static IServiceCollection AddSecureSignCore(this IServiceCollection services, IConfiguration config)
	    {
		    services.AddDataProtection()
				.SetApplicationName("SecureSign")
				// Share keys between the CLI app and the web service, by using the same path.
				.PersistKeysToFileSystem(new DirectoryInfo(GetEncryptionKeyPathOrThrow(config)));
		    services.AddSingleton<IPasswordGenerator, PasswordGenerator>();
			services.AddSingleton<ISecretStorage, SecretStorage>();
		    services.AddSingleton<IAccessTokenSerializer, AccessTokenSerializer>();
		    services.AddSingleton<IAuthenticodeSigner, AuthenticodeSigner>();

			// Configuration
		    services.Configure<PathConfig>(config.GetSection("Paths"));
		    services.Configure<Dictionary<string, AccessTokenConfig>>(config.GetSection("AccessTokens"));

			return services;
	    }

		/// <summary>
		/// Gets the path that the encryption keys are stored in, or throws an exception if not available.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
	    private static string GetEncryptionKeyPathOrThrow(IConfiguration config)
	    {
		    var path = config["Paths:EncryptionKeys"];
		    if (string.IsNullOrWhiteSpace(path))
		    {
			    throw new InvalidOperationException("encryptionKeys path was not properly configured!");
		    }
		    return path;

	    }
    }
}
