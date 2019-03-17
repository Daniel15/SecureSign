/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System.Security.Cryptography.X509Certificates;

namespace SecureSign.Core
{
	/// <summary>
	/// Handles storing secret files, such as encryption keys. These files are encrypted at rest (on disk).
	/// </summary>
	public interface ISecretStorage
	{
		/// <summary>
		/// Saves an Authenticode certificate into the secret store, using the specified code (encryption key)
		/// </summary>
		/// <param name="name">Name of the secret file</param>
		/// <param name="cert">Certificate to save</param>
		/// <param name="code">Encryption key to encrypt the secret file with</param>
		void SaveSecret(string name, X509Certificate2 cert, string code);

		/// <summary>
		/// Saves a secret file using the specified code (encryption key)
		/// </summary>
		/// <param name="name">Name of the secret file</param>
		/// <param name="contents">Contents of the secret file</param>
		/// <param name="code">Encryption key to encrypt the secret file with</param>
		void SaveSecret(string name, byte[] contents, string code);

		/// <summary>
		/// Loads an Authenticode certificate from the secret store.
		/// </summary>
		/// <param name="name">File to load</param>
		/// <param name="code">Encryption key that was used to encrypt the secret file</param>
		/// <returns>The file</returns>
		X509Certificate2 LoadAuthenticodeCertificate(string name, string code);

		/// <summary>
		/// Loads a secret file
		/// </summary>
		/// <param name="name">File to load</param>
		/// <param name="code">Encryption key that was used to encrypt the secret file</param>
		/// <returns>The file</returns>
		byte[] LoadSecret(string name, string code);


		/// <summary>
		/// Gets the path that the specified secret file would be saved to
		/// </summary>
		/// <param name="name">Name of the secret file</param>
		/// <returns>Full file path for the secret file</returns>
		string GetPathForSecret(string name);

		/// <summary>
		/// Throws an exception if a secret with the specified name already exists.
		/// </summary>
		/// <param name="name">Name of the secret file</param>
		void ThrowIfSecretExists(string name);
	}
}
