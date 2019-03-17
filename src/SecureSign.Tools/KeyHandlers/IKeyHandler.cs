/**
 * Copyright (c) 2019 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using SecureSign.Core;
using SecureSign.Core.Models;

namespace SecureSign.Tools.KeyHandlers
{
	/// <summary>
	/// Handles storing new secret keys and creating access tokens for them
	/// </summary>
	public interface IKeyHandler
	{
		/// <summary>
		/// Gets the key type that this key handler supports
		/// </summary>
		KeyType KeyType { get; }

		/// <summary>
		/// Adds a new key to the secret storage.
		/// </summary>
		/// <param name="inputPath"></param>
		void AddKey(string inputPath);

		/// <summary>
		/// Creates a new access token to use the specified key
		/// </summary>
		/// <param name="code">Encryption code for the key</param>
		/// <param name="name">Name of the key</param>
		/// <returns>Access token and its config</returns>
		(AccessToken accessToken, AccessTokenConfig accessTokenConfig) CreateAccessToken(
			string code, 
			string name
		);
	}
}
