// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Duende.IdentityServer.Validation;

namespace Duende.IdentityServer.Hosting.LocalApiAuthentication
{
    /// <summary>
    /// Authentication handler for validating access token from the local IdentityServer
    /// </summary>
    public class LocalApiAuthenticationHandler : AuthenticationHandler<LocalApiAuthenticationOptions>
    {
        private readonly ITokenValidator _tokenValidator;
        private readonly ILogger _logger;

        /// <inheritdoc />
        public LocalApiAuthenticationHandler(IOptionsMonitor<LocalApiAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, ITokenValidator tokenValidator)
            : base(options, logger, encoder, clock)
        {
            _tokenValidator = tokenValidator;
            _logger = logger.CreateLogger<LocalApiAuthenticationHandler>();
        }

        /// <summary>
        /// The handler calls methods on the events which give the application control at certain points where processing is occurring. 
        /// If it is not provided a default instance is supplied which does nothing when the methods are called.
        /// </summary>
        protected new LocalApiAuthenticationEvents Events
        {
            get => (LocalApiAuthenticationEvents)base.Events;
            set => base.Events = value;
        }

        /// <inheritdoc/>
        protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new LocalApiAuthenticationEvents());

        /// <inheritdoc />
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            _logger.LogTrace("HandleAuthenticateAsync called");

            string token = null;

            string authorization = Request.Headers["Authorization"];

            if (string.IsNullOrEmpty(authorization))
            {
                return AuthenticateResult.NoResult();
            }

            if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authorization.Substring("Bearer ".Length).Trim();
            }

            if (string.IsNullOrEmpty(token))
            {
                return AuthenticateResult.Fail("No Access Token is sent.");
            }

            _logger.LogTrace("Token found: {token}", token);

            TokenValidationResult result = await _tokenValidator.ValidateAccessTokenAsync(token, Options.ExpectedScope);

            if (result.IsError)
            {
                _logger.LogTrace("Failed to validate the token");

                return AuthenticateResult.Fail(result.Error);
            }

            _logger.LogTrace("Successfully validated the token.");

            var claimsIdentity = new ClaimsIdentity(result.Claims, Scheme.Name, JwtClaimTypes.Name, JwtClaimTypes.Role);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            var authenticationProperties = new AuthenticationProperties();

            if (Options.SaveToken)
            {
                authenticationProperties.StoreTokens(new[]
                {
                    new AuthenticationToken { Name = "access_token", Value = token }
                });
            }

            var claimsTransformationContext = new ClaimsTransformationContext
            {
                Principal = claimsPrincipal,
                HttpContext = Context
            };

            await Events.ClaimsTransformation(claimsTransformationContext);

            var authenticationTicket = new AuthenticationTicket(claimsTransformationContext.Principal, authenticationProperties, Scheme.Name);
            return AuthenticateResult.Success(authenticationTicket);
        }
    }
}