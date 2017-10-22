/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System.IO;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using SecureSign.Core.Signers;

namespace SecureSign.Core.Extensions
{
	/// <summary>
	/// Adds SecureSign services to the dependency injection container
	/// </summary>
    public static class ServiceCollectionExtensions
    {
	    public static IServiceCollection AddSecureSignCore(this IServiceCollection services)
	    {
		    services.AddDataProtection()
				.SetApplicationName("SecureSign")
				// Share keys between the CLI app and the web service
				// TODO Remove hardcoded path
				.PersistKeysToFileSystem(new DirectoryInfo(@"C:\src\SecureSign\keys"));
		    services.AddSingleton<IPasswordGenerator, PasswordGenerator>();
			services.AddSingleton<ISecretStorage, SecretStorage>();
		    services.AddSingleton<IAccessTokenSerializer, AccessTokenSerializer>();
		    services.AddSingleton<IAuthenticodeSigner, AuthenticodeSigner>();
			return services;
	    }
    }
}
