/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SecureSign.Core.Exceptions;
using SecureSign.Core.Extensions;
using SecureSign.Core.Models;

namespace SecureSign.Core.Signers
{
	/// <summary>
	/// A signer implementation that signs files with an Authenticode signature.
	/// </summary>
	public class AuthenticodeSigner : IAuthenticodeSigner, IDisposable
	{
		private const int TIMEOUT = 60_000;

		private readonly IPasswordGenerator _passwordGenerator;
		private readonly PathConfig _pathConfig;
		/// <summary>
		/// Files to delete AFTER the request completes.
		/// </summary>
		private readonly List<string> _filesToDelete = new List<string>();

		/// <summary>
		/// Creates a new <see cref="AuthenticodeSigner"/>.
		/// </summary>
		/// <param name="passwordGenerator"></param>
		public AuthenticodeSigner(IPasswordGenerator passwordGenerator, IOptions<PathConfig> pathConfig)
		{
			_passwordGenerator = passwordGenerator;
			_pathConfig = pathConfig.Value;
		}

		/// <summary>
		/// Signs the provided resource with an Authenticode signature.
		/// </summary>
		/// <param name="input">Object to sign</param>
		/// <param name="cert">Certificate to use for signing</param>
		/// <param name="description">Description to sign the object with</param>
		/// <param name="url">URL to include in the signature</param>
		/// <returns>A signed copy of the file</returns>
		public async Task<Stream> SignAsync(Stream input, X509Certificate2 cert, string description, string url, string fileExtention)
		{
			// Temporarily save the cert to disk with a random password, as osslsigncode needs to read it from disk.
			var password = _passwordGenerator.Generate();
			var inputFile = Path.GetTempFileName() + fileExtention;
			var certFile = Path.GetTempFileName();
			_filesToDelete.Add(inputFile);

			try
			{
				var exportedCert = cert.Export(X509ContentType.Pfx, password);
				File.WriteAllBytes(certFile, exportedCert);
				await input.CopyToFileAsync(inputFile);

				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					if (fileExtention.Contains("ps"))
					{
						return await SignUsingPowerShellAsync(inputFile, certFile, password);
					}
					else
					{
						return await SignUsingSignToolAsync(inputFile, certFile, password, description, url);
					}
				}
				else
				{
					return await SignUsingOpenSsl(inputFile, certFile, password, description, url);
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
		private async Task<Stream> SignUsingSignToolAsync(string inputFile, string certFile, string certPassword, string description, string url)
		{
			await RunProcessAsync(
				_pathConfig.SignTool,
				new[]
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
				}
			);

			// SignTool signs in-place, so just return the file we were given.
			return File.OpenRead(inputFile);
		}

		/// <summary>
		/// Signs the specified file using Powershell Set-Authenticode
		/// </summary>
		/// <param name="inputFile">File to sign</param>
		/// <param name="certFile">Path to the certificate to use for signing</param>
		/// <param name="certPassword">Password for the certificate</param>
		/// <returns>A signed copy of the file</returns>
		private async Task<Stream> SignUsingPowerShellAsync(string inputFile, string certFile, string certPassword)
		{
			await RunProcessAsync(
				"powershell.exe",
				new[]
				{
					"-command",
					"\"$Cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2;",
					$"$Cert.Import('{CommandLineEncoder.Utils.EncodeArgText(certFile)}','{CommandLineEncoder.Utils.EncodeArgText(certPassword)}',[System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::DefaultKeySet);",
					$"Set-AuthenticodeSignature '{CommandLineEncoder.Utils.EncodeArgText(inputFile)}' $Cert -Timestamp http://timestamp.digicert.com\"",
				}
			);

			// PowerShell signs in-place, so just return the file we were given.
			return File.OpenRead(inputFile);
		}

		/// <summary>
		/// Signs the specified file using osslsigncode
		/// </summary>
		/// <param name="inputFile">File to sign</param>
		/// <param name="certFile">Path to the certificate to use for signing</param>
		/// <param name="certPassword">Password for the certificate</param>
		/// <param name="description">Description to sign the object with</param>
		/// <param name="url">URL to include in the signature</param>
		/// <returns>A signed copy of the file</returns>
		private async Task<Stream> SignUsingOpenSsl(string inputFile, string certFile, string certPassword,
			string description, string url)
		{
			var outputFile = Path.GetTempFileName();
			_filesToDelete.Add(outputFile);

			// Command-line arguments can be shown in the output of "ps". Therefore, we don't want to pass
			// the certificate's password at the command-line. Instead, save it into a temp file that's
			// deleted once the signing has been completed
			var certPasswordFile = Path.GetTempFileName();

			try
			{
				File.WriteAllText(certPasswordFile, certPassword);

				// Windows 7 and 10 have deprecated SHA1 and require SHA256 or higher,
				// however Vista and XP don't support SHA256. To fix this, we sign using
				// *both* methods (dual signing).
				// Reference: http://www.elstensoftware.com/blog/2016/02/10/dual-signing-osslsigncode/

				// First sign with SHA1
				await RunOsslSignCodeAsync(certFile, certPasswordFile, description, url, new[]
				{
					"-h sha1",
					$"-in \"{CommandLineEncoder.Utils.EncodeArgText(inputFile)}\"",
					$"-out \"{CommandLineEncoder.Utils.EncodeArgText(outputFile)}\"",
				});

				// Now sign with SHA256
				await RunOsslSignCodeAsync(certFile, certPasswordFile, description, url, new[]
				{
					"-nest",
					"-h sha2",
					$"-in \"{CommandLineEncoder.Utils.EncodeArgText(outputFile)}\"",
					$"-out \"{CommandLineEncoder.Utils.EncodeArgText(outputFile)}\"",
				});

				return File.OpenRead(outputFile);
			}
			finally
			{
				File.Delete(certPasswordFile);
			}
		}

		private async Task RunOsslSignCodeAsync(string certFile, string certPasswordFile, string description, string url, string[] extraArgs)
		{
			var args = new List<string>
			{
				"sign",
				"-ts http://timestamp.digicert.com",
				$"-n \"{CommandLineEncoder.Utils.EncodeArgText(description)}\"",
				$"-i \"{CommandLineEncoder.Utils.EncodeArgText(url)}\"",
				$"-pkcs12 \"{CommandLineEncoder.Utils.EncodeArgText(certFile)}\"",
				$"-readpass \"{CommandLineEncoder.Utils.EncodeArgText(certPasswordFile)}\""
			};
			await RunProcessAsync("osslsigncode", args.Concat(extraArgs).ToArray());
		}

		/// <summary>
		/// Runs an external process and waits it to return.
		/// </summary>
		/// <param name="appName">Executeable to run</param>
		/// <param name="args">Arguments to pass</param>
		/// <exception cref="AuthenticodeFailedException">If a non-zero error code is returned</exception>
		private async Task RunProcessAsync(string appName, params string[] args)
		{
			var process = new Process
			{
				StartInfo =
				{
					FileName = appName,
					Arguments = string.Join(" ", args),
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
		}

		public void Dispose()
		{
			foreach (var file in _filesToDelete)
			{
				File.Delete(file);
			}
		}
	}
}
