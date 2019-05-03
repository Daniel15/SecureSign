/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SecureSign.Core;
using SecureSign.Core.Signers;
using SecureSign.Web.Models;

namespace SecureSign.Web.Controllers
{
	/// <summary>
	/// Handles requests to sign files
	/// </summary>
	[Produces("application/json")]
	public class AuthenticodeSigningController : Controller
	{
		private readonly ISecretStorage _secretStorage;
		private readonly IAuthenticodeSigner _signer;
		private readonly SigningControllerUtils _utils;
		

		public AuthenticodeSigningController(
			ISecretStorage secretStorage,
			IAuthenticodeSigner signer,
			SigningControllerUtils utils
		)
		{
			_secretStorage = secretStorage;
			_signer = signer;
			_utils = utils;
		}

		/// <summary>
		/// Handles requests to sign files with an Authenticode signature.
		/// </summary>
		/// <param name="request">Details of the request</param>
		/// <returns>Authenticode-signed file</returns>
		[Route("sign/authenticode")]
		public async Task<IActionResult> SignUsingAuthenticode(SignRequest request)
		{
		var (token, tokenConfig, tokenError) = _utils.TryGetAccessToken(request);
		if (tokenError != null)
		{
			return tokenError;
		}

		var cert = _secretStorage.LoadAuthenticodeCertificate(token.KeyName, token.Code);
		var (artifact, artifactError, fileExtention) = await _utils.GetFileFromPayloadAsync(token, tokenConfig, request);
		if (artifactError != null)
		{
			return artifactError;
		}
            
		var signed = await _signer.SignAsync(artifact, cert, tokenConfig.SignDescription, tokenConfig.SignUrl, fileExtention);
		return File(signed, "application/octet-stream");
		}
	}
}
