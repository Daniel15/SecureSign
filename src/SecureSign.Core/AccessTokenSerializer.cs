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
		    return _protector.Protect(JsonConvert.SerializeObject(token));
	    }

	    /// <summary>
	    /// Decrypts and deserializes the provided access token
	    /// </summary>
	    /// <param name="token">Access token to decrypt</param>
	    /// <returns>Decrypted access token</returns>
		public AccessToken Deserialize(string token)
	    {
		    var raw = _protector.Unprotect(token);
		    return JsonConvert.DeserializeObject<AccessToken>(raw);
	    }
    }
}
