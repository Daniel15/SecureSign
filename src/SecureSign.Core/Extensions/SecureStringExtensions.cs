/**
 * Copyright (c) 2019 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SecureSign.Core.Extensions
{
	public static class SecureStringExtensions
	{
		public static string ToAnsiString(this SecureString value)
		{
			var valuePtr = IntPtr.Zero;
			try
			{
				valuePtr = Marshal.SecureStringToGlobalAllocAnsi(value);
				return Marshal.PtrToStringAnsi(valuePtr);
			}
			finally
			{
				Marshal.ZeroFreeGlobalAllocAnsi(valuePtr);
			}
		}
	}
}
