/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System.IO;

namespace SecureSign.Core.Models
{
	/// <summary>
	/// Defines configuration for various file paths
	/// </summary>
    public class PathConfig
	{
		/// <summary>
		/// File name access token data is stored in
		/// </summary>
		public const string ACCESS_TOKEN_FILENAME = "accessTokenConfig.json";

		/// <summary>
		/// The root directory for the SecureSign app
		/// </summary>
		public string Root { get; set; }

		/// <summary>
		/// Gets or sets the path to store encryption keys
		/// </summary>
		public string EncryptionKeys { get; set; }

		/// <summary>
		/// Gets or sets the path to store Authenticode certificates
		/// </summary>
		public string Certificates { get; set; }

		/// <summary>
		/// Gets the full path that access token information is stored in
		/// </summary>
		public string AccessTokenConfig => Path.Combine(Root, ACCESS_TOKEN_FILENAME);

		/// <summary>
		/// Gets the full path to SecureSign's GnuPG home directory
		/// </summary>
		public string GpgHome => Path.Combine(Root, ".gnupg");

		/// <summary>
		/// Gets or sets the path to signtool.exe
		/// </summary>
	    public string SignTool { get; set; } = @"C:\Program Files (x86)\Windows Kits\10\bin\10.0.17763.0\x86\signtool.exe";

    }
}
