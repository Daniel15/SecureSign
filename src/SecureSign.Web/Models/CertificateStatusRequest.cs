/**
 * Copyright (c) 2019 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

namespace SecureSign.Web.Models
{
	/// <summary>
	/// Represents a request to get the status of a signing certificate.
	/// </summary>
	public class CertificateStatusRequest
	{
		/// <summary>
		/// Gets or sets the access token for the request. Will decode to an instance of <see cref="AccessToken"/>.
		/// </summary>
		public string AccessToken { get; set; }
	}
}
