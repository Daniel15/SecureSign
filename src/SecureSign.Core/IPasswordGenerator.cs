/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

namespace SecureSign.Core
{
	/// <summary>
	/// Handles generation of random alphanumeric strings
	/// </summary>
    public interface IPasswordGenerator
    {
		/// <summary>
		/// Generate a cryptographically secure alphanumeric string.
		/// </summary>
	    string Generate();
    }
}
