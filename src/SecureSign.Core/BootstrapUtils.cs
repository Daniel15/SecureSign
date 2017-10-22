/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System.IO;
using SecureSign.Core.Exceptions;

namespace SecureSign.Core
{
	/// <summary>
	/// Utilities for starting the SecureSign system
	/// </summary>
    public static class BootstrapUtils
	{
		private const string FILE_IN_ROOT = "appsettings.json";

		/// <summary>
		/// Determines the root directory for the SecureSign app. This is a bit tricky because the
		/// directory structure in dev (with separate SecureSign.Web and SEcureSign.Tools directories)
		/// differs from prod (where everything is in one directory).
		/// </summary>
		/// <returns>Root directory</returns>
		/// <exception cref="InvalidConfigurationException">Thrown if root can not be determined</exception>
	    public static string DetermineRootDirectory()
	    {
		    var current = new DirectoryInfo(Directory.GetCurrentDirectory());

		    do
		    {
			    if (File.Exists(Path.Combine(current.FullName, FILE_IN_ROOT)))
			    {
				    return current.FullName;
			    }
			    current = current.Parent;
		    } while (current != null);

		    throw new InvalidConfigurationException("Could not determine root directory");
	    }
    }
}
