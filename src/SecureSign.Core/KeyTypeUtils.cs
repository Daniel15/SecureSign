/**
 * Copyright (c) 2019 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.IO;

namespace SecureSign.Core
{
	public static class KeyTypeUtils
	{
		/// <summary>
		/// Gets the <see cref="KeyType"/> that this file could contain.
		/// </summary>
		/// <param name="filename">Name of the file</param>
		/// <returns>The key type</returns>
		public static KeyType FromFilename(string filename)
		{
			switch (Path.GetExtension(filename))
			{
				case ".pfx":
					return KeyType.Authenticode;
				case ".gpg":
					return KeyType.Gpg;
				default:
					throw new Exception("Unsupported key type");
			}
		}
	}
}
