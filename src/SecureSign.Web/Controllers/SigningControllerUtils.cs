/**
 * Copyright (c) 2019 Daniel Lo Nigro (Daniel15)
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
using SecureSign.Web.Models;

namespace SecureSign.Web.Controllers
{
	/// <summary>
	/// Utilities specific to signing controllers.
	/// </summary>
	public class SigningControllerUtils
	{
		private readonly ILogger<SigningControllerUtils> _logger;
		private readonly IAccessTokenSerializer _accessTokenSerializer;
		private readonly Dictionary<string, AccessTokenConfig> _accessTokenConfig;
		private readonly HttpClient _httpClient = new HttpClient();

		public SigningControllerUtils(
			IAccessTokenSerializer accessTokenSerializer,
			IOptions<Dictionary<string, AccessTokenConfig>> accessTokenConfig,
			ILogger<SigningControllerUtils> logger
		)
		{
			_logger = logger;
			_accessTokenSerializer = accessTokenSerializer;
			_accessTokenConfig = accessTokenConfig.Value;
		}

		public (AccessToken, AccessTokenConfig, IActionResult) TryGetAccessToken(SignRequest request)
		{
			AccessToken token;
			try
			{
				token = _accessTokenSerializer.Deserialize(request.AccessToken);
			}
			catch (Exception ex)
			{
				_logger.LogInformation(ex, "Access token could not be decrypted");
				return (null, null, new UnauthorizedResult());
			}

			if (!_accessTokenConfig.TryGetValue(token.Id, out var tokenConfig) || !tokenConfig.Valid)
			{
				_logger.LogWarning("Access token not in config file, or marked as invalid: {Id}", token.Id);
				return (null, null, new UnauthorizedResult());
			}

			return (token, tokenConfig, null);
		}

		/// <summary>
		/// Gets the file to sign from the request payload.
		/// </summary>
		/// <param name="token">Access token for the request</param>
		/// <param name="tokenConfig">Configuration for this access token</param>
		/// <param name="request">The request</param>
		/// <returns>The file contents</returns>
		public async Task<(byte[], IActionResult, string)> GetFileFromPayloadAsync(AccessToken token, AccessTokenConfig tokenConfig, SignRequest request)
		{
			try
			{
				if (!CheckIfRequestIsWhitelisted(token, tokenConfig, request))
				{
					throw new InvalidOperationException("Upload request is not allowed.");
				}

				if (request.ArtifactUrl != null)
				{
					_logger.LogInformation("Signing request received: {Id} is signing {ArtifactUrl}", token.Id,
						request.ArtifactUrl);
					var artifact = await _httpClient.GetByteArrayAsync(request.ArtifactUrl);
                    var fileExtension = Path.GetExtension(request.ArtifactUrl.AbsolutePath);
                    return (artifact, null, fileExtension);
				}

				if (request.Artifact != null)
				{
					_logger.LogInformation("Signing request received: {Id} is signing {Filename}", token.Id,
						request.Artifact.FileName);
					using (var stream = new MemoryStream())
					{
						await request.Artifact.CopyToAsync(stream);
                        var fileExtension = Path.GetExtension(request.Artifact.FileName);
                        return (stream.ToArray(), null, fileExtension);
					}
				}

				return (null, new BadRequestObjectResult("No artifact found to sign"), null);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Could not retrieve artifact to sign");
				return (null, new BadRequestObjectResult(ex.Message), null);
			}
		}

		private bool CheckIfRequestIsWhitelisted(AccessToken token, AccessTokenConfig tokenConfig, SignRequest request)
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
