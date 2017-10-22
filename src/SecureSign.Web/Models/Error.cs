/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

namespace SecureSign.Web.Models
{
	/// <summary>
	/// Represents an error that occurred during the request
	/// </summary>
    public class Error
    {
		/// <summary>
		/// Gets or sets the error message.
		/// </summary>
	    public string Message { get; set; }

		/// <summary>
		/// Gets or sets the stack trace of the error (in dev).
		/// </summary>
		public string Stack { get; set; }
    }
}
