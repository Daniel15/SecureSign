/**
 * Copyright (c) 2019 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SecureSign.Core;
using SecureSign.Core.Models;
using SecureSign.Web.Models;

namespace SecureSign.Web.Controllers
{
	[Produces("application/json")]
	[Route("certstatus")]
    public class CertificateStatusController : Controller
    {
	    private readonly IAccessTokenSerializer _accessTokenSerializer;
	    private readonly ILogger<CertificateStatusController> _logger;
	    private readonly ISecretStorage _secretStorage;

		public CertificateStatusController(ILogger<CertificateStatusController> logger, IAccessTokenSerializer accessTokenSerializer, ISecretStorage secretStorage)
	    {
		    _logger = logger;
		    _accessTokenSerializer = accessTokenSerializer;
		    _secretStorage = secretStorage;
	    }

	    [Route("")]
        public IActionResult Index(CertificateStatusRequest request)
        {
			AccessToken token;
			try
			{
				token = _accessTokenSerializer.Deserialize(request.AccessToken);
			}
			catch (Exception ex)
			{
				_logger.LogInformation(ex, "Access token could not be decrypted");
				return Unauthorized();
			}

			var cert = _secretStorage.LoadAuthenticodeCertificate(token.KeyName, token.Code);
			return Ok(new CertificateStatusResponse
			{
				CreationDate = cert.NotBefore,
				ExpiryDate = cert.NotAfter,
				Issuer = cert.IssuerName.Format(false),
				Name = cert.FriendlyName,
				SerialNumber = cert.SerialNumber,
				Subject = cert.SubjectName.Format(false),
				Thumbprint = cert.Thumbprint,
			});
        }
    }
}