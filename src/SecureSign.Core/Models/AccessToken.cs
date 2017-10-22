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
	/// </summary>
	[Serializable]
    public class AccessToken
    {
		/// <summary>
		/// Time this access token was issued at 
		/// </summary>
		public DateTime IssuedAt { get; set; }

		/// <summary>
		/// Name of the key to use for signing requests using this access token
		/// </summary>
		public string KeyName { get; set; }

		/// <summary>
		/// Decrpytion key for the key
		/// </summary>
		public string Code { get; set; }

		/// <summary>
		/// An optional comment describing what this access token is used for. Not used by SecureSign, but can
		/// be used for tracking access token usages.
		/// </summary>
		public string Comment { get; set; }

		/// <summary>
		/// URL to include in Authenticode metadata for signing requests performed with this access token
		/// </summary>
		public string SignUrl { get; set; }

		/// <summary>
		/// Description to include in Authenticode metadata for signing requests performed with this access token
		/// </summary>
		public string SignDescription { get; set; }
    }
}
