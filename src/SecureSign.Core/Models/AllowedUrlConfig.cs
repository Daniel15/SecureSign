/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

namespace SecureSign.Core.Models
{
	/// <summary>
	/// Configuration data for a URL whitelist
	/// </summary>
    public class AllowedUrlConfig
    {
		/// <summary>
		/// Gets or sets the whitelisted domain pattern
		/// </summary>
		public string Domain { get; set; }

		/// <summary>
		/// Gets or sets the whitelisted URL pattern
		/// </summary>
		public string Path { get; set; }
    }
}
