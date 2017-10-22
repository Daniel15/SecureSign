/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using SecureSign.Core.Exceptions;

namespace SecureSign.Core.Signers
{
	/// <summary>
	/// A signer implementation that signs files with an Authenticode signature.
	/// </summary>
	public class AuthenticodeSigner : IAuthenticodeSigner
	{
		private const int TIMEOUT = 10000;

		private readonly IPasswordGenerator _passwordGenerator;
		private readonly HttpClient _client = new HttpClient();

		/// <summary>
		/// Creates a new <see cref="AuthenticodeSigner"/>.
		/// </summary>
		/// <param name="passwordGenerator"></param>
		public AuthenticodeSigner(IPasswordGenerator passwordGenerator)
		{
			_passwordGenerator = passwordGenerator;
		}

		/// <summary>
		/// Signs the resource located at the specified URI with an Authenticode signature.
		/// </summary>
		/// <param name="artifactUri">URI of the object to sign</param>
		/// <param name="cert">Certificate to use for signing</param>
		/// <param name="description">Description to sign the object with</param>
		/// <param name="url">URL to include in the signature</param>
		/// <returns>A signed copy of the file</returns>
		public async Task<byte[]> SignAsync(Uri artifactUri, X509Certificate2 cert, string description, string url)
		{
			var artifact = await _client.GetByteArrayAsync(artifactUri);
			return await SignAsync(artifact, cert, description, url);
		}

		/// <summary>
		/// Signs the provided resource with an Authenticode signature.
		/// </summary>
		/// <param name="input">Object to sign</param>
		/// <param name="cert">Certificate to use for signing</param>
		/// <param name="description">Description to sign the object with</param>
		/// <param name="url">URL to include in the signature</param>
		/// <returns>A signed copy of the file</returns>
		public async Task<byte[]> SignAsync(byte[] input, X509Certificate2 cert, string description, string url)
		{
			// Temporarily save the cert to disk with a random password, as osslsigncode needs to read it from disk.
			var password = _passwordGenerator.Generate();
			var inputFile = Path.GetTempFileName();
			var certFile = Path.GetTempFileName();

			try
			{
				var exportedCert = cert.Export(X509ContentType.Pfx, password);
				File.WriteAllBytes(certFile, exportedCert);
				File.WriteAllBytes(inputFile, input);

				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					return await SignUsingSignToolAsync(inputFile, certFile, password, description, url);
				}
				else
				{
					throw new NotImplementedException();
				}

			}
			finally
			{
				File.Delete(certFile);
			}
		}

		/// <summary>
		/// Signs the specified file using signtool.exe from the Windows SDK
		/// </summary>
		/// <param name="inputFile">File to sign</param>
		/// <param name="certFile">Path to the certificate to use for signing</param>
		/// <param name="certPassword">Password for the certificate</param>
		/// <param name="description">Description to sign the object with</param>
		/// <param name="url">URL to include in the signature</param>
		/// <returns>A signed copy of the file</returns>
		private async Task<byte[]> SignUsingSignToolAsync(string inputFile, string certFile, string certPassword, string description, string url)
		{
			var process = new Process
			{
				StartInfo =
				{
					// TODO: This path shouldn't be hard-coded
					FileName = @"C:\Program Files (x86)\Windows Kits\8.1\bin\x86\signtool.exe",
					Arguments = string.Join(" ", new[]
					{
						"sign",
						"/q",
						$"/f \"{CommandLineEncoder.Utils.EncodeArgText(certFile)}\"",
						$"/p \"{CommandLineEncoder.Utils.EncodeArgText(certPassword)}\"",
						$"/d \"{CommandLineEncoder.Utils.EncodeArgText(description)}\"",
						$"/du \"{CommandLineEncoder.Utils.EncodeArgText(url)}\"",
						"/tr http://timestamp.digicert.com",
						"/td sha256",
						"/fd sha256",
						$"\"{CommandLineEncoder.Utils.EncodeArgText(inputFile)}\"",
					}),
					CreateNoWindow = true,
					RedirectStandardError = true,
					RedirectStandardOutput = true,
					UseShellExecute = false,
				}
			};
			process.Start();
			process.WaitForExit(TIMEOUT);

			if (process.ExitCode != 0)
			{
				var errorOutput = await process.StandardError.ReadToEndAsync();
				throw new AuthenticodeFailedException("Failed to Authenticode sign: " + errorOutput);
			}

			// SignTool signs in-place, so just return the file we were given.
			return File.ReadAllBytes(inputFile);
		}
	}
}
