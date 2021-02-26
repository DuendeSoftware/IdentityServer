// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using IdentityModel;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Validates JWT authorization request objects
    /// </summary>
    public class JwtRequestValidator : IJwtRequestValidator
    {
        private readonly string _audienceUri;

        /// <summary>
        /// JWT handler
        /// </summary>
        protected JsonWebTokenHandler Handler;

        /// <summary>
        /// The audience URI to use
        /// </summary>
        protected async Task<string> GetAudienceUri()
        {
            if (_audienceUri.IsPresent())
            {
                return _audienceUri;
            }

            return await IssuerNameService.GetCurrentAsync();
        }

        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// The options
        /// </summary>
        protected readonly IdentityServerOptions Options;

        /// <summary>
        /// The issuer name service
        /// </summary>
        protected readonly IIssuerNameService IssuerNameService;

        /// <summary>
        /// Instantiates an instance of private_key_jwt secret validator
        /// </summary>
        public JwtRequestValidator(IdentityServerOptions options, IIssuerNameService issuerNameService,
            ILogger<JwtRequestValidator> logger)
        {
            Options = options;
            IssuerNameService = issuerNameService;
            Logger = logger;
            
            Handler = new JsonWebTokenHandler
            {
                MaximumTokenSizeInBytes = options.InputLengthRestrictions.Jwt
            };
        }

        /// <summary>
        /// Instantiates an instance of private_key_jwt secret validator (used for testing)
        /// </summary>
        internal JwtRequestValidator(string audience, ILogger<JwtRequestValidator> logger)
        {
            _audienceUri = audience;
            
            Logger = logger;
            Handler = new JsonWebTokenHandler();
        }

        /// <summary>
        /// Validates a JWT request object
        /// </summary>
        /// <param name="client">The client</param>
        /// <param name="jwtTokenString">The JWT</param>
        /// <returns></returns>
        public virtual async Task<JwtRequestValidationResult> ValidateAsync(Client client, string jwtTokenString)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (String.IsNullOrWhiteSpace(jwtTokenString)) throw new ArgumentNullException(nameof(jwtTokenString));

            var fail = new JwtRequestValidationResult { IsError = true };

            List<SecurityKey> trustedKeys;
            try
            {
                trustedKeys = await GetKeysAsync(client);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Could not parse client secrets");
                return fail;
            }

            if (!trustedKeys.Any())
            {
                Logger.LogError("There are no keys available to validate JWT.");
                return fail;
            }

            JsonWebToken jwtSecurityToken;
            try
            {
                jwtSecurityToken = await ValidateJwtAsync(jwtTokenString, trustedKeys, client);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "JWT token validation error");
                return fail;
            }

            if (jwtSecurityToken.TryGetPayloadValue<string>(OidcConstants.AuthorizeRequest.Request, out _) ||
                jwtSecurityToken.TryGetPayloadValue<string>(OidcConstants.AuthorizeRequest.RequestUri, out _))
            {
                Logger.LogError("JWT payload must not contain request or request_uri");
                return fail;
            }

            var payload = await ProcessPayloadAsync(jwtSecurityToken);

            var result = new JwtRequestValidationResult
            {
                IsError = false,
                Payload = payload
            };

            Logger.LogDebug("JWT request object validation success.");
            return result;
        }

        /// <summary>
        /// Retrieves keys for a given client
        /// </summary>
        /// <param name="client">The client</param>
        /// <returns></returns>
        protected virtual Task<List<SecurityKey>> GetKeysAsync(Client client)
        {
            return client.ClientSecrets.GetKeysAsync();
        }

        /// <summary>
        /// Validates the JWT token
        /// </summary>
        /// <param name="jwtTokenString">JWT as a string</param>
        /// <param name="keys">The keys</param>
        /// <param name="client">The client</param>
        /// <returns></returns>
        protected virtual async Task<JsonWebToken> ValidateJwtAsync(string jwtTokenString, IEnumerable<SecurityKey> keys,
            Client client)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKeys = keys,
                ValidateIssuerSigningKey = true,

                ValidIssuer = client.ClientId,
                ValidateIssuer = true,

                ValidAudience = await GetAudienceUri(),
                ValidateAudience = true,

                RequireSignedTokens = true,
                RequireExpirationTime = true
            };

            if (Options.StrictJarValidation)
            {
                tokenValidationParameters.ValidTypes = new[] { JwtClaimTypes.JwtTypes.AuthorizationRequest };
            }

            var result = Handler.ValidateToken(jwtTokenString, tokenValidationParameters);
            if (!result.IsValid)
            {
                throw result.Exception;
            }

            return (JsonWebToken)result.SecurityToken;
        }

        /// <summary>
        /// Processes the JWT contents
        /// </summary>
        /// <param name="token">The JWT token</param>
        /// <returns></returns>
        protected virtual Task<List<Claim>> ProcessPayloadAsync(JsonWebToken token)
        {
            // filter JWT validation values
            var filtered = token.Claims.Where(claim => !Constants.Filters.JwtRequestClaimTypesFilter.Contains(claim.Type));
            return Task.FromResult(filtered.ToList());
        }
    }
}