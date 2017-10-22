/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;

namespace SecureSign.Core.Extensions
{
	/// <summary>
	/// Extension methods relating to GUIDs.
	/// </summary>
	public static class GuidExtensions
	{
		/// <summary>
		/// Returns a short identifier for this GUID.
		/// </summary>
		/// <param name="guid">The unique identifier.</param>
		/// <returns>A short version of the unique identifier</returns>
		public static string ToShortGuid(this Guid guid)
		{
			return Convert.ToBase64String(guid.ToByteArray())
				.Replace("/", string.Empty)
				.Replace("+", string.Empty)
				.TrimEnd('=');
		}
	}
}
