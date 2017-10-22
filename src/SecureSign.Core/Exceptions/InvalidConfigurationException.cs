/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;

namespace SecureSign.Core.Exceptions
{
	/// <summary>
	/// Thrown when SecureSign is configured incorrectly.
	/// </summary>
    public class InvalidConfigurationException : Exception
    {
	    public InvalidConfigurationException(string message) : base(message)
	    {
	    }
	}
}
