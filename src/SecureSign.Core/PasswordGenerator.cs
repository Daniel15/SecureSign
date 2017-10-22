/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.Security.Cryptography;

namespace SecureSign.Core
{
	public class PasswordGenerator : IPasswordGenerator
	{
		/// <summary>
		/// Generate a cryptographically secure alphanumeric string.
		/// </summary>
		public string Generate()
		{
			var rng = RandomNumberGenerator.Create();
			byte[] keyBytes = new byte[40];
			rng.GetBytes(keyBytes);
			return Convert.ToBase64String(keyBytes)
				.Replace("=", string.Empty)
				.Replace("/", string.Empty)
				.Replace("=", string.Empty);
		}
	}
}
