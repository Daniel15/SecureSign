using System;

namespace SecureSign.Core.Exceptions
{
	/// <summary>
	/// Thrown when Authenticode signing fails.
	/// </summary>
    public class AuthenticodeFailedException : Exception
    {
	    public AuthenticodeFailedException(string message) : base(message)
	    {
	    }
    }
}
