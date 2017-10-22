/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using SecureSign.Core.Models;

namespace SecureSign.Core
{
	/// <summary>
	/// Handles storing secret files, such as encryption keys. These files are encrypted at rest (on disk).
	/// </summary>
	public class SecretStorage : ISecretStorage
    {
	    private readonly IDataProtector _protector;
	    private readonly PathConfig _pathConfig;

	    /// <summary>
		/// Creates a new <see cref="SecretStorage"/>, using the specified <see cref="IDataProtectionProvider"/> 
		/// to encrypt the secret files.
		/// </summary>
		/// <param name="provider">Provider to encrypt the secrets</param>
	    public SecretStorage(IDataProtectionProvider provider, IOptions<PathConfig> pathConfig)
		{
			_protector = provider.CreateProtector("SecureSign.SecretStorage");
			_pathConfig = pathConfig.Value;
		}

	    /// <summary>
	    /// Saves an Authenticode certificate into the secret store, using the specified code (encryption key)
	    /// </summary>
	    /// <param name="name">Name of the secret file</param>
	    /// <param name="cert">Certificate to save</param>
	    /// <param name="code">Encryption key to encrypt the secret file with</param>
		public void SaveSecret(string name, X509Certificate2 cert, string code)
	    {
		    var encryptedCert = cert.Export(X509ContentType.Pfx, code);
			SaveSecret(name, encryptedCert, code);
	    }

	    /// <summary>
	    /// Saves a secret file using the specified code (encryption key)
	    /// </summary>
	    /// <param name="name">Name of the secret file</param>
	    /// <param name="contents">Contents of the secret file</param>
	    /// <param name="code">Encryption key to encrypt the secret file with</param>
		public void SaveSecret(string name, byte[] contents, string code)
	    {
		    var path = GetPathForSecret(name);
			var protector = CreateProtector(code);
			var encryptedKey = protector.Protect(contents);
		    Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllBytes(path, encryptedKey);
	    }

	    /// <summary>
	    /// Loads an Authenticode certificate from the secret store.
	    /// </summary>
	    /// <param name="name">File to load</param>
	    /// <param name="code">Encryption key that was used to encrypt the secret file</param>
	    /// <returns>The file</returns>
		public X509Certificate2 LoadAuthenticodeCertificate(string name, string code)
	    {
		    var rawCert = LoadSecret(name, code);
		    return new X509Certificate2(rawCert, code, X509KeyStorageFlags.Exportable);
	    }

		/// <summary>
		/// Loads a secret file
		/// </summary>
		/// <param name="name">File to load</param>
		/// <param name="code">Encryption key that was used to encrypt the secret file</param>
		/// <returns>The file</returns>
		public byte[] LoadSecret(string name, string code)
	    {
		    var path = GetPathForSecret(name);
		    var protector = CreateProtector(code);
		    return protector.Unprotect(File.ReadAllBytes(path));
	    }

	    /// <summary>
	    /// Gets the path that the specified secret file would be saved to
	    /// </summary>
	    /// <param name="name">Name of the secret file</param>
	    /// <returns>Full file path for the secret file</returns>
		public string GetPathForSecret(string name)
	    {
		    return Path.Combine(_pathConfig.Certificates, name);
	    }

		/// <summary>
		/// Creates an <see cref="IDataProtector"/> to encrypt files with the specified key.
		/// </summary>
		/// <param name="encryptionKey">Key to use</param>
		/// <returns>The data protector</returns>
	    private IDataProtector CreateProtector(string encryptionKey)
	    {
		    return _protector.CreateProtector($"Key: {encryptionKey}");
	    }
    }
}
