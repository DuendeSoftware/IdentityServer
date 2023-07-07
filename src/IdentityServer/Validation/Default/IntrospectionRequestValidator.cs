// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;
using IdentityModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static Duende.IdentityServer.Constants;

namespace Duende.IdentityServer.Validation;

/// <summary>
/// The introspection request validator
/// </summary>
/// <seealso cref="IIntrospectionRequestValidator" />
internal class IntrospectionRequestValidator : IIntrospectionRequestValidator
{
    private readonly ILogger _logger;
    private readonly ITokenValidator _tokenValidator;
    private readonly IRefreshTokenService _refreshTokenService;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntrospectionRequestValidator"/> class.
    /// </summary>
    /// <param name="tokenValidator">The token validator.</param>
    /// <param name="refreshTokenService"></param>
    /// <param name="logger">The logger.</param>
    public IntrospectionRequestValidator(ITokenValidator tokenValidator, IRefreshTokenService refreshTokenService, ILogger<IntrospectionRequestValidator> logger)
    {
        _tokenValidator = tokenValidator;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IntrospectionRequestValidationResult> ValidateAsync(IntrospectionRequestValidationContext context)
    {
        var parameters = context.Parameters;
        var api = context.Api;
        var client = context.Client;

        if (api != null && client != null)
        {
            throw new ArgumentException("Can't validate with both an ApiResource and a Client.");
        }
        if (api == null && client == null)
        {
            throw new ArgumentException("Can't validate without an ApiResource or a Client.");
        }

        using var activity = Tracing.BasicActivitySource.StartActivity("IntrospectionRequestValidator.Validate");
        
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
                Client = client,
                Error = "missing_token",
                Parameters = parameters
            };
        }

        var hint = parameters.Get("token_type_hint");
        if (hint.IsPresent())
        {
            if (Constants.SupportedTokenTypeHints.Contains(hint))
            {
                _logger.LogDebug("Token type hint found in request: {tokenTypeHint}", hint);
            }
            else
            {
                _logger.LogError("Invalid token type hint: {tokenTypeHint}", hint);
                return new IntrospectionRequestValidationResult
                {
                    IsError = true,
                    Api = api,
                    Client = client,
                    Error = "invalid_request",
                    Parameters = parameters
                };
            }
        }

        // the result we care about
        IEnumerable<Claim> claims = null;

        if (api != null)
        {
            // if we have an API calling, then the token should only ever be an access token
            if (hint.IsMissing() || hint == TokenTypeHints.AccessToken)
            {
                // validate token
                var tokenValidationResult = await _tokenValidator.ValidateAccessTokenAsync(token);

                // success
                if (!tokenValidationResult.IsError)
                {
                    _logger.LogDebug("Validated access token");
                    claims = tokenValidationResult.Claims;
                }
            }
        }
        else
        {
            // clients can pass either token type
            if (hint.IsMissing() || hint == TokenTypeHints.AccessToken)
            {
                // try access token
                var tokenValidationResult = await _tokenValidator.ValidateAccessTokenAsync(token);
                if (!tokenValidationResult.IsError)
                {
                    var list = tokenValidationResult.Claims.ToList();

                    var tokenClientId = list.SingleOrDefault(x => x.Type == JwtClaimTypes.ClientId)?.Value;
                    if (tokenClientId == client.ClientId)
                    {
                        _logger.LogDebug("Validated access token");
                        list.Add(new Claim("token_type", TokenTypeHints.AccessToken));
                        claims = list;
                    }
                }
            }

            if (claims == null)
            {
                // we get in here if hint is for refresh token, or the access token lookup failed
                var refreshValidationResult = await _refreshTokenService.ValidateRefreshTokenAsync(token, client);
                if (!refreshValidationResult.IsError)
                {
                    _logger.LogDebug("Validated refresh token");
                    
                    var iat = refreshValidationResult.RefreshToken.CreationTime.ToEpochTime();
                    var list = new List<Claim>
                    {
                        new Claim("client_id", client.ClientId),
                        new Claim("token_type", TokenTypeHints.RefreshToken),
                        new Claim("iat", iat.ToString(), ClaimValueTypes.Integer),
                        new Claim("exp", (iat + refreshValidationResult.RefreshToken.Lifetime).ToString(), ClaimValueTypes.Integer),
                        new Claim("sub", refreshValidationResult.RefreshToken.SubjectId),
                    };

                    foreach (var scope in refreshValidationResult.RefreshToken.AuthorizedScopes)
                    {
                        list.Add(new Claim("scope", scope));
                    }

                    claims = list;
                }
            }
        }
        

        if (claims != null)
        {
            _logger.LogDebug("Introspection request validation successful.");

            return new IntrospectionRequestValidationResult
            {
                IsActive = true,
                IsError = false,
                Token = token,
                Api = api,
                Client = client,
                Claims = claims,
                Parameters = parameters
            };
        }

        // if we get here then fail
        _logger.LogDebug("Token is invalid.");

        return new IntrospectionRequestValidationResult
        {
            IsActive = false,
            IsError = false,
            Token = token,
            Api = api,
            Client = client,
            Parameters = parameters
        };
    }
}