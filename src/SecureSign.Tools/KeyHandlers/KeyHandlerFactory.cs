/**
 * Copyright (c) 2019 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using SecureSign.Core;

namespace SecureSign.Tools.KeyHandlers
{
	/// <summary>
	/// Handles getting the key factory to handle the given file
	/// </summary>
	class KeyHandlerFactory : IKeyHandlerFactory
	{
		private readonly IEnumerable<IKeyHandler> _handlers;

		public KeyHandlerFactory(IEnumerable<IKeyHandler> handlers)
		{
			_handlers = handlers;
		}

		/// <summary>
		/// Gets the handler for the specified file. If no handler supports this file, throws an exception.
		/// </summary>
		/// <param name="filename">Name of the key file</param>
		/// <returns>The handler for this key file</returns>
		public IKeyHandler GetHandler(string filename)
		{
			try
			{
				var keytype = KeyTypeUtils.FromFilename(filename);
				return _handlers.First(x => x.KeyType == keytype);
			}
			catch (Exception ex)
			{
				throw new Exception($"Unrecognised file extension. Please use .pfx for Authenticode or .gpg for GPG. {ex.Message}");
			}
		}
	}
}
