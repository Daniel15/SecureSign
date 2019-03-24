/**
 * Copyright (c) 2019 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

namespace SecureSign.Core
{
	/// <summary>
	/// Represents a type of signing key supported by SecureSign
	/// </summary>
	public enum KeyType
	{
		/// <summary>
		/// Authenticode (eg. to sign Windows installers)
		/// </summary>
		Authenticode,

		/// <summary>
		/// GnuPG
		/// </summary>
		Gpg,
	}
}
