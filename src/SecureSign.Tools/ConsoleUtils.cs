/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.Security;

namespace SecureSign.Tools
{
	/// <summary>
	/// Various console utilities.
	/// </summary>
    public static class ConsoleUtils
    {
		/// <summary>
		/// Show a prompt to allow the user to enter some input
		/// </summary>
		/// <param name="prompt">Prompt to show. A question mark will be shown after this prompt.</param>
		/// <returns>Value entered by the user</returns>
	    public static string Prompt(string prompt)
	    {
		    Console.Write(prompt + "? ");
		    return Console.ReadLine();
	    }

		/// <summary>
		/// Prompts the user for a password in a secure manner.
		/// </summary>
		/// <param name="prompt">Prompt to show. A question mark will be shown after this prompt.</param>
		/// <returns>Password that was entered by the user</returns>
		public static SecureString PasswordPrompt(string prompt)
	    {
		    Console.Write(prompt + "? ");
		    var password = new SecureString();
		    while (true)
		    {
			    var key = Console.ReadKey(true);
			    switch (key.Key)
			    {
				    case ConsoleKey.Enter:
					    Console.WriteLine();
					    return password;

				    case ConsoleKey.Backspace:
					    if (password.Length > 0)
					    {
						    password.RemoveAt(password.Length - 1);
						    Console.Write("\b \b"); // Delete the asterisk
					    }
					    break;

				    default:
					    password.AppendChar(key.KeyChar);
					    Console.Write("*");
					    break;
			    }
		    }
		}
    }
}
