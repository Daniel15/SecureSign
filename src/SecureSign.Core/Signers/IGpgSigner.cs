/**
 * Copyright (c) 2019 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

namespace SecureSign.Core.Signers
{
	/// <summary>
	/// Handles signing artifacts using GPG.
	/// </summary>
	public interface IGpgSigner
	{
		/// <summary>
		/// Signs the specified artifact using GPG
		/// </summary>
		/// <param name="input">Bytes to sign</param>
		/// <param name="keygrip">Hex representation of keygrip</param>
		/// <param name="secretKeyFile">Contents of the secret key file</param>
		/// <param name="fingerprint">Fingerprint of the key</param>
		/// <returns>ASCII-armored signature</returns>
		byte[] Sign(byte[] input, string keygrip, byte[] secretKeyFile, string fingerprint);
	}
}
