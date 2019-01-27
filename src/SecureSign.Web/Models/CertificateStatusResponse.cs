/**
 * Copyright (c) 2019 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;

namespace SecureSign.Web.Models
{
	/// <summary>
	/// Represents the status of a signing certificate.
	/// </summary>
	public class CertificateStatusResponse
	{
		/// <summary>
		/// The date this certificate is valid from.
		/// </summary>
		public DateTime CreationDate { get; set; }

		/// <summary>
		/// The date this certificate is valid from, as a UNIX timestamp
		/// </summary>
		public long CreationDateUnix => ((DateTimeOffset) CreationDate).ToUnixTimeSeconds();

		/// <summary>
		/// The date this certificate expires.
		/// </summary>
		public DateTime ExpiryDate { get; set; }

		/// <summary>
		/// The date this certificate expires, as a UNIX timestamp
		/// </summary>
		public long ExpiryDateUnix => ((DateTimeOffset)ExpiryDate).ToUnixTimeSeconds();

		/// <summary>
		/// The friendly name of the issuer of the certificate.
		/// </summary>
		public string Issuer { get; set; }

		/// <summary>
		/// The friendly name of the certificate.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The serial number of the certificate.
		/// </summary>
		public string SerialNumber { get; set; }

		/// <summary>
		/// The subject of the certificate.
		/// </summary>
		public string Subject { get; set; }

		/// <summary>
		/// The thumbprint of the certificate.
		/// </summary>
		public string Thumbprint { get; set; }
	}
}
