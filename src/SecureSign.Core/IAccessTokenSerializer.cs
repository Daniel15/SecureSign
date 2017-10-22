/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using SecureSign.Core.Models;

namespace SecureSign.Core
{
	/// <summary>
	/// Handles encrypting and decrypting access tokens.
	/// </summary>
    public interface IAccessTokenSerializer
    {
		/// <summary>
		/// Serializes and encrypts the provided access token
		/// </summary>
		/// <param name="token">Access token to encrypt</param>
		/// <returns>Encrypted access token</returns>
	    string Serialize(AccessToken token);


		/// <summary>
		/// Decrypts and deserializes the provided access token
		/// </summary>
		/// <param name="token">Access token to decrypt</param>
		/// <returns>Decrypted access token</returns>
	    AccessToken Deserialize(string token);
    }
}
