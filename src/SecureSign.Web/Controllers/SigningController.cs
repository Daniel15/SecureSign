/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SecureSign.Core;
using SecureSign.Core.Models;
using SecureSign.Core.Signers;
using SecureSign.Web.Models;

namespace SecureSign.Web.Controllers
{
	/// <summary>
	/// Handles requests to sign files
	/// </summary>
	[Produces("application/json")]
	[Route("sign")]
	public class SigningController : Controller
	{
		private readonly ISecretStorage _secretStorage;
		private readonly IAccessTokenSerializer _accessTokenSerializer;
		private readonly IAuthenticodeSigner _signer;

		public SigningController(ISecretStorage secretStorage, IAccessTokenSerializer accessTokenSerializer, IAuthenticodeSigner signer)
		{
			_secretStorage = secretStorage;
			_accessTokenSerializer = accessTokenSerializer;
			_signer = signer;
		}

		/// <summary>
		/// Handles requests to sign files with an Authenticode signature.
		/// </summary>
		/// <param name="request">Details of the request</param>
		/// <returns>Authenticode-signed file</returns>
		[Route("authenticode")]
		public async Task<IActionResult> SignUsingAuthenticode([FromBody] AuthenticodeSignRequest request)
		{
			AccessToken token;
			try
			{
				token = _accessTokenSerializer.Deserialize(request.AccessToken);
			}
			catch (Exception ex)
			{
				// TODO: log `ex`
				return Unauthorized();
			}

			var cert = _secretStorage.LoadAuthenticodeCertificate(token.KeyName, token.Code);
			var signed = await _signer.SignAsync(request.ArtifactUrl, cert, token.SignDescription, token.SignUrl);
			return File(signed, "application/octet-stream");
		}
	}
}
