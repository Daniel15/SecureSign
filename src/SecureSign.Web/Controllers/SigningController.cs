/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
		private readonly ILogger<SigningController> _logger;
		private readonly IDictionary<string, AccessTokenConfig> _accessTokenConfig;
		private readonly HttpClient _httpClient = new HttpClient();

		public SigningController(
			ISecretStorage secretStorage,
			IAccessTokenSerializer accessTokenSerializer,
			IAuthenticodeSigner signer,
			IOptions<Dictionary<string, AccessTokenConfig>> accessTokenConfig,
			ILogger<SigningController> logger
		)
		{
			_secretStorage = secretStorage;
			_accessTokenConfig = accessTokenConfig.Value;
			_accessTokenSerializer = accessTokenSerializer;
			_signer = signer;
			_logger = logger;
		}

		/// <summary>
		/// Handles requests to sign files with an Authenticode signature.
		/// </summary>
		/// <param name="request">Details of the request</param>
		/// <returns>Authenticode-signed file</returns>
		[Route("authenticode")]
		public async Task<IActionResult> SignUsingAuthenticode(AuthenticodeSignRequest request)
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

			if (!_accessTokenConfig.TryGetValue(token.Id, out var tokenConfig) || !tokenConfig.Valid)
			{
				_logger.LogWarning("Access token not in config file, or marked as invalid: {Id}", token.Id);
				return Unauthorized();
			}

			var cert = _secretStorage.LoadAuthenticodeCertificate(token.KeyName, token.Code);
			byte[] artifact;
			try
			{
				artifact = await GetFileFromPayloadAsync(token, tokenConfig, request);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Could not retrieve artifact to sign");
				return BadRequest(ex.Message);
			}
			
			var signed = await _signer.SignAsync(artifact, cert, tokenConfig.SignDescription, tokenConfig.SignUrl);
			return File(signed, "application/octet-stream");
		}

		/// <summary>
		/// Gets the file to sign from the request payload.
		/// </summary>
		/// <param name="token">Access token for the request</param>
		/// <param name="tokenConfig">Configuration for this access token</param>
		/// <param name="request">The request</param>
		/// <returns>The file contents</returns>
		private async Task<byte[]> GetFileFromPayloadAsync(AccessToken token, AccessTokenConfig tokenConfig, AuthenticodeSignRequest request)
		{
			if (!CheckIfRequestIsWhitelisted(token, tokenConfig, request))
			{
				throw new InvalidOperationException("Upload request is not allowed.");
			}
			if (request.ArtifactUrl != null)
			{
				_logger.LogInformation("Signing request received: {Id} is signing {ArtifactUrl}", token.Id, request.ArtifactUrl);
				return await _httpClient.GetByteArrayAsync(request.ArtifactUrl);
			}

			if (request.Artifact != null)
			{
				_logger.LogInformation("Signing request received: {Id} is signing {Filename}", token.Id, request.Artifact.FileName);
				using (var stream = new MemoryStream())
				{
					await request.Artifact.CopyToAsync(stream);
					return stream.ToArray();
				}
			}

			// TODO: This should likely throw instead
			return new byte[0];
		}

		private bool CheckIfRequestIsWhitelisted(AccessToken token, AccessTokenConfig tokenConfig, AuthenticodeSignRequest request)
		{
			if (
				request.ArtifactUrl != null &&
				tokenConfig.AllowedUrls != null &&
				!tokenConfig.AllowedUrls.Any(item =>
					Regex.IsMatch(request.ArtifactUrl.Host, item.Domain) &&
					Regex.IsMatch(request.ArtifactUrl.AbsolutePath, item.Path)
				)
			)
			{
				_logger.LogWarning("[{Id}] URL signing requested, but url {Url} is not on the whitelist!", token.Id, request.ArtifactUrl);
				return false;
			}

			if (request.Artifact != null && !tokenConfig.AllowUploads)
			{
				_logger.LogWarning("[{Id}] File uploaded, but access token is forbidden from doing so!", token.Id);
				return false;
			}

			return true;
		}
	}
}
