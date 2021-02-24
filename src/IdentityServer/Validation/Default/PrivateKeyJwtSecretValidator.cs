// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Validates a secret based on RS256 signed JWT token
    /// </summary>
    public class PrivateKeyJwtSecretValidator : ISecretValidator
    {
        private readonly IIssuerNameService _issuerNameService;
        private readonly IReplayCache _replayCache;
        private readonly IdentityServerOptions _options;
        private readonly ILogger _logger;

        private const string Purpose = nameof(PrivateKeyJwtSecretValidator);

        /// <summary>
        /// Instantiates an instance of private_key_jwt secret validator
        /// </summary>
        public PrivateKeyJwtSecretValidator(
            IIssuerNameService issuerNameService, 
            IReplayCache replayCache,
            IdentityServerOptions options,
            ILogger<PrivateKeyJwtSecretValidator> logger)
        {
            _issuerNameService = issuerNameService;
            _replayCache = replayCache;
            _options = options;
            _logger = logger;
        }

        /// <summary>
        /// Validates a secret
        /// </summary>
        /// <param name="secrets">The stored secrets.</param>
        /// <param name="parsedSecret">The received secret.</param>
        /// <returns>
        /// A validation result
        /// </returns>
        /// <exception cref="System.ArgumentException">ParsedSecret.Credential is not a JWT token</exception>
        public async Task<SecretValidationResult> ValidateAsync(IEnumerable<Secret> secrets, ParsedSecret parsedSecret)
        {
            var fail = new SecretValidationResult { Success = false };
            var success = new SecretValidationResult { Success = true };

            if (parsedSecret.Type != IdentityServerConstants.ParsedSecretTypes.JwtBearer)
            {
                return fail;
            }

            if (!(parsedSecret.Credential is string jwtTokenString))
            {
                _logger.LogError("ParsedSecret.Credential is not a string.");
                return fail;
            }

            List<SecurityKey> trustedKeys;
            try
            {
                trustedKeys = await secrets.GetKeysAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not parse secrets");
                return fail;
            }

            if (!trustedKeys.Any())
            {
                _logger.LogError("There are no keys available to validate client assertion.");
                return fail;
            }

            var validAudiences = new[]
            {
                // token endpoint URL
                string.Concat((await _issuerNameService.GetCurrentAsync()).EnsureTrailingSlash(),
                    Constants.ProtocolRoutePaths.Token)
            };

            var tokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKeys = trustedKeys,
                ValidateIssuerSigningKey = true,

                ValidIssuer = parsedSecret.Id,
                ValidateIssuer = true,

                ValidAudiences = validAudiences,
                ValidateAudience = true,

                RequireSignedTokens = true,
                RequireExpirationTime = true,

                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var handler = new JsonWebTokenHandler() { MaximumTokenSizeInBytes = _options.InputLengthRestrictions.Jwt };
            var result = handler.ValidateToken(jwtTokenString, tokenValidationParameters);
            if (!result.IsValid)
            {
                _logger.LogError(result.Exception, "JWT token validation error");
                return fail;
            }

            var jwtToken = (JsonWebToken) result.SecurityToken;
            if (jwtToken.Subject != jwtToken.Issuer)
            {
                _logger.LogError("Both 'sub' and 'iss' in the client assertion token must have a value of client_id.");
                return fail;
            }

            var exp = jwtToken.ValidTo;
            if (exp == DateTime.MinValue)
            {
                _logger.LogError("exp is missing.");
                return fail;
            }

            var jti = jwtToken.Id;
            if (jti.IsMissing())
            {
                _logger.LogError("jti is missing.");
                return fail;
            }

            if (await _replayCache.ExistsAsync(Purpose, jti))
            {
                _logger.LogError("jti is found in replay cache. Possible replay attack.");
                return fail;
            }
            else
            {
                await _replayCache.AddAsync(Purpose, jti, exp.AddMinutes(5));
            }

            return success;
        }
    }
}