// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// The introspection request validator
    /// </summary>
    /// <seealso cref="IIntrospectionRequestValidator" />
    internal class IntrospectionRequestValidator : IIntrospectionRequestValidator
    {
        private readonly ILogger _logger;
        private readonly ITokenValidator _tokenValidator;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntrospectionRequestValidator"/> class.
        /// </summary>
        /// <param name="tokenValidator">The token validator.</param>
        /// <param name="logger">The logger.</param>
        public IntrospectionRequestValidator(ITokenValidator tokenValidator, ILogger<IntrospectionRequestValidator> logger)
        {
            _tokenValidator = tokenValidator;
            _logger = logger;
        }

        /// <summary>
        /// Validates the request.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="api">The API.</param>
        /// <returns></returns>
        public async Task<IntrospectionRequestValidationResult> ValidateAsync(NameValueCollection parameters, ApiResource api)
        {
            _logger.LogDebug("Introspection request validation started.");

            // retrieve required token
            var token = parameters.Get("token");
            if (token == null)
            {
                _logger.LogError("Token is missing");

                return new IntrospectionRequestValidationResult
                {
                    IsError = true,
                    Api = api,
                    Error = "missing_token",
                    Parameters = parameters
                };
            }

            // validate token
            var tokenValidationResult = await _tokenValidator.ValidateAccessTokenAsync(token);

            // invalid or unknown token
            if (tokenValidationResult.IsError)
            {
                _logger.LogDebug("Token is invalid.");

                return new IntrospectionRequestValidationResult
                {
                    IsActive = false,
                    IsError = false,
                    Token = token,
                    Api = api,
                    Parameters = parameters
                };
            }

            _logger.LogDebug("Introspection request validation successful.");

            // valid token
            return new IntrospectionRequestValidationResult
            {
                IsActive = true,
                IsError = false,
                Token = token,
                Claims = tokenValidationResult.Claims,
                Api = api,
                Parameters = parameters
            };
        }
    }
}