/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.Collections.Generic;

namespace SecureSign.Core.Models
{
	/// <summary>
	/// Configuration data for a particular access token
	/// </summary>
	public class AccessTokenConfig
	{
		/// <summary>
		/// Time this access token was issued at 
		/// </summary>
		public DateTime IssuedAt { get; set; }

		/// <summary>
		/// Whether this access token is allowed to be used
		/// </summary>
		public bool Valid { get; set; } = true;

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

		/// <summary>
		/// Gets or sets whether direct file uploads are allowed
		/// </summary>
		public bool AllowUploads { get; set; } = true;

		/// <summary>
		/// Gets or sets the list of URLs allowed to be signed by this access token
		/// </summary>
		public IList<AllowedUrlConfig> AllowedUrls { get; set; }
	}
}
