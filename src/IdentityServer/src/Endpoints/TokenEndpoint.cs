// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using IdentityModel;
using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Duende.IdentityServer.Endpoints.Results;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;

namespace Duende.IdentityServer.Endpoints
{
    /// <summary>
    /// The token endpoint
    /// </summary>
    /// <seealso cref="IEndpointHandler" />
    internal class TokenEndpoint : IEndpointHandler
    {
        private readonly IClientSecretValidator _clientValidator;
        private readonly ITokenRequestValidator _requestValidator;
        private readonly ITokenResponseGenerator _responseGenerator;
        private readonly IEventService _events;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenEndpoint" /> class.
        /// </summary>
        /// <param name="clientValidator">The client validator.</param>
        /// <param name="requestValidator">The request validator.</param>
        /// <param name="responseGenerator">The response generator.</param>
        /// <param name="events">The events.</param>
        /// <param name="logger">The logger.</param>
        public TokenEndpoint(
            IClientSecretValidator clientValidator, 
            ITokenRequestValidator requestValidator, 
            ITokenResponseGenerator responseGenerator, 
            IEventService events, 
            ILogger<TokenEndpoint> logger)
        {
            _clientValidator = clientValidator;
            _requestValidator = requestValidator;
            _responseGenerator = responseGenerator;
            _events = events;
            _logger = logger;
        }

        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns></returns>
        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            _logger.LogTrace("Processing token request.");

            // validate HTTP
            if (!HttpMethods.IsPost(context.Request.Method) || !context.Request.HasApplicationFormContentType())
            {
                _logger.LogWarning("Invalid HTTP request for token endpoint");
                return Error(OidcConstants.TokenErrors.InvalidRequest);
            }

            return await ProcessTokenRequestAsync(context);
        }

        private async Task<IEndpointResult> ProcessTokenRequestAsync(HttpContext context)
        {
            _logger.LogDebug("Start token request.");

            // validate client
            var clientResult = await _clientValidator.ValidateAsync(context);

            if (clientResult.Client == null)
            {
                return Error(OidcConstants.TokenErrors.InvalidClient);
            }

            // validate request
            var form = (await context.Request.ReadFormAsync()).AsNameValueCollection();
            _logger.LogTrace("Calling into token request validator: {type}", _requestValidator.GetType().FullName);
            var requestResult = await _requestValidator.ValidateRequestAsync(form, clientResult);

            if (requestResult.IsError)
            {
                await _events.RaiseAsync(new TokenIssuedFailureEvent(requestResult));
                return Error(requestResult.Error, requestResult.ErrorDescription, requestResult.CustomResponse);
            }

            // create response
            _logger.LogTrace("Calling into token request response generator: {type}", _responseGenerator.GetType().FullName);
            var response = await _responseGenerator.ProcessAsync(requestResult);

            await _events.RaiseAsync(new TokenIssuedSuccessEvent(response, requestResult));
            LogTokens(response, requestResult);

            // return result
            _logger.LogDebug("Token request success.");
            return new TokenResult(response);
        }

        private TokenErrorResult Error(string error, string errorDescription = null, Dictionary<string, object> custom = null)
        {
            var response = new TokenErrorResponse
            {
                Error = error,
                ErrorDescription = errorDescription,
                Custom = custom
            };

            return new TokenErrorResult(response);
        }

        private void LogTokens(TokenResponse response, TokenRequestValidationResult requestResult)
        {
            var clientId = $"{requestResult.ValidatedRequest.Client.ClientId} ({requestResult.ValidatedRequest.Client?.ClientName ?? "no name set"})";
            var subjectId = requestResult.ValidatedRequest.Subject?.GetSubjectId() ?? "no subject";

            if (response.IdentityToken != null)
            {
                _logger.LogTrace("Identity token issued for {clientId} / {subjectId}: {token}", clientId, subjectId, response.IdentityToken);
            }
            if (response.RefreshToken != null)
            {
                _logger.LogTrace("Refresh token issued for {clientId} / {subjectId}: {token}", clientId, subjectId, response.RefreshToken);
            }
            if (response.AccessToken != null)
            {
                _logger.LogTrace("Access token issued for {clientId} / {subjectId}: {token}", clientId, subjectId, response.AccessToken);
            }
        }
    }
}