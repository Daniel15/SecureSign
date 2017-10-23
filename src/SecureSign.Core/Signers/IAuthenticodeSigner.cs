/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace SecureSign.Core.Signers
{
	/// <summary>
	/// A signer implementation that signs files with an Authenticode signature.
	/// </summary>
	public interface IAuthenticodeSigner
	{
		/// <summary>
		/// Signs the provided resource with an Authenticode signature.
		/// </summary>
		/// <param name="input">Object to sign</param>
		/// <param name="cert">Certificate to use for signing</param>
		/// <param name="description">Description to sign the object with</param>
		/// <param name="url">URL to include in the signature</param>
		/// <returns>A signed copy of the file</returns>
		Task<byte[]> SignAsync(byte[] input, X509Certificate2 cert, string description, string url);
	}
}