/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;

namespace SecureSign.Core.Models
{
	/// <summary>
	/// Represents an access token for SecureSign. An access token contains the encryption key to decrypt the
	/// key to use for the request, as well as some other metadata about how the key should be used.
	/// This class only contains data that is directly serialized into the access token.
	/// </summary>
	[Serializable]
    public class AccessToken
    {
		/// <summary>
		/// Unique ID for this access token
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Time this access token was issued at 
		/// </summary>
		public DateTime IssuedAt { get; set; }

		/// <summary>
		/// Name of the key to use for signing requests using this access token.
		/// For GPG, this is the keygrip.
		/// </summary>
		public string KeyName { get; set; }

		/// <summary>
		/// Fingerprint of the key to use for signing. Only used for GPG.
		/// </summary>
		public string KeyFingerprint { get; set; }

		/// <summary>
		/// Decryption key for the key
		/// </summary>
		public string Code { get; set; }
    }
}
