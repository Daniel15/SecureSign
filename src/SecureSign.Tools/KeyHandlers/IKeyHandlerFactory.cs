/**
 * Copyright (c) 2019 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

namespace SecureSign.Tools.KeyHandlers
{
	/// <summary>
	/// Handles getting the key factory to handle the given file
	/// </summary>
	public interface IKeyHandlerFactory
	{
		/// <summary>
		/// Gets the handler for the specified file. If no handler supports this file, throws an exception.
		/// </summary>
		/// <param name="filename">Name of the key file</param>
		/// <returns>The handler for this key file</returns>
		IKeyHandler GetHandler(string filename);
	}
}
