/**
 * Copyright (c) 2019 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
			var extension = Path.GetExtension(filename);
			var handler = _handlers.FirstOrDefault(x => x.FileExtension == extension);
			if (handler == null)
			{
				throw CreateUnsupportedHandlerException();
			}

			return handler;
		}

		/// <summary>
		/// Creates an exception detailing which file extensions are supported by the currently registered handlers.
		/// </summary>
		/// <returns>Exception</returns>
		private Exception CreateUnsupportedHandlerException()
		{
			var message = new StringBuilder();
			message.Append("Unrecognised file extension. Please use one of the following:\n");
			foreach (var supportedHandler in _handlers)
			{
				message.AppendFormat($"- {supportedHandler.FileExtension}: {supportedHandler.GetType().Name}\n");
			}
			return new Exception(message.ToString());
		}
	}
}
