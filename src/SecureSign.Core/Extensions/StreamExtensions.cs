/**
 * Copyright (c) 2019 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System.IO;
using System.Threading.Tasks;

namespace SecureSign.Core.Extensions
{
	/// <summary>
	/// Extensions for <see cref="Stream"/>.
	/// </summary>
	public static class StreamExtensions
	{
		/// <summary>
		/// Writes the stream to the specified file path.
		/// </summary>
		/// <param name="stream">Stream to write</param>
		/// <param name="filename">File to write to</param>
		public static void CopyToFile(this Stream stream, string filename)
		{
			using (var fileStream = File.Create(filename))
			{
				stream.Seek(0, SeekOrigin.Begin);
				stream.CopyTo(fileStream);
			}
		}

		/// <summary>
		/// Writes the stream to the specified file path.
		/// </summary>
		/// <param name="stream">Stream to write</param>
		/// <param name="filename">File to write to</param>
		public static async Task CopyToFileAsync(this Stream stream, string filename)
		{
			using (var fileStream = File.Create(filename))
			{
				if(stream.CanSeek)
				{
					stream.Seek(0, SeekOrigin.Begin);
				}
				await stream.CopyToAsync(fileStream);
			}
		}
	}
}
