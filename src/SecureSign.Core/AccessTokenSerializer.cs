/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;
using SecureSign.Core.Models;

namespace SecureSign.Core
{
    public class AccessTokenSerializer : IAccessTokenSerializer
    {
	    private const string ID_SEPARATOR = "-";

	    private readonly IDataProtector _protector;

	    public AccessTokenSerializer(IDataProtectionProvider provider)
	    {
		    _protector = provider.CreateProtector("SecureSign.AccessToken");
	    }

	    /// <summary>
	    /// Serializes and encrypts the provided access token
	    /// </summary>
	    /// <param name="token">Access token to encrypt</param>
	    /// <returns>Encrypted access token</returns>
		public string Serialize(AccessToken token)
	    {
		    var encrypted = _protector.Protect(JsonConvert.SerializeObject(token));
		    return token.Id + ID_SEPARATOR + encrypted;
	    }

	    /// <summary>
	    /// Decrypts and deserializes the provided access token
	    /// </summary>
	    /// <param name="token">Access token to decrypt</param>
	    /// <returns>Decrypted access token</returns>
		public AccessToken Deserialize(string token)
	    {
			// Strip ID from the start
		    var firstHyphen = token.IndexOf(ID_SEPARATOR);
		    token = token.Substring(firstHyphen + 1);
		    var raw = _protector.Unprotect(token);
		    return JsonConvert.DeserializeObject<AccessToken>(raw);
	    }
    }
}
