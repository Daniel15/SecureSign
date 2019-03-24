/**
 * Copyright (c) 2019 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.IO;
using Libgpgme;
using Microsoft.Extensions.Options;
using SecureSign.Core.Models;

namespace SecureSign.Core.Signers
{
	/// <summary>
	/// Handles signing artifacts using GPG.
	/// </summary>
	public class GpgSigner : IGpgSigner
	{
		private readonly Context _ctx;
		private readonly PathConfig _pathConfig;
		private static readonly object _signLock = new object();

		public GpgSigner(IOptions<PathConfig> pathConfig, Context ctx)
		{
			_ctx = ctx;
			_pathConfig = pathConfig.Value;
		}

		/// <summary>
		/// Signs the specified artifact using GPG
		/// </summary>
		/// <param name="input">Bytes to sign</param>
		/// <param name="keygrip">Hex representation of keygrip</param>
		/// <param name="secretKeyFile">Contents of the secret key file</param>
		/// <param name="fingerprint">Fingerprint of the key</param>
		/// <returns>ASCII-armored signature</returns>
		public byte[] Sign(byte[] input, string keygrip, byte[] secretKeyFile, string fingerprint)
		{
			// Since this signer messes with some global state (private keys in the GPG home directory),
			// we must lock to ensure no other signing requests happen concurrently.
			lock (_signLock)
			{
				var keygripPath = Path.Combine(_pathConfig.GpgHome, "private-keys-v1.d", keygrip + ".key");
				var inputPath = Path.GetTempFileName();
				try
				{
					File.WriteAllBytes(keygripPath, secretKeyFile);
					File.WriteAllBytes(inputPath, input);

					var key = _ctx.KeyStore.GetKey(fingerprint, secretOnly: true);
					_ctx.Signers.Clear();
					_ctx.Signers.Add(key);
					_ctx.Armor = true;

					using (var inputData = new GpgmeFileData(inputPath, FileMode.Open, FileAccess.Read))
					using (var sigData = new GpgmeMemoryData { FileName = "signature.asc" })
					{
						var result = _ctx.Sign(inputData, sigData, SignatureMode.Detach);
						if (result.InvalidSigners != null)
						{
							throw new Exception($"Could not sign: {result.InvalidSigners.Reason}");
						}

						using (var memStream = new MemoryStream())
						{
							sigData.Seek(0, SeekOrigin.Begin);
							sigData.CopyTo(memStream);
							return memStream.GetBuffer();
						}
					}
				}
				finally
				{
					File.Delete(keygripPath);
					File.Delete(inputPath);
				}
			}
		}
	}
}
