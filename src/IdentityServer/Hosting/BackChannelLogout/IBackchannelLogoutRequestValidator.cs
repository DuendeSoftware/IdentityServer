// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using IdentityModel;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Threading;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;
using System.Text.Json;
using Duende.IdentityServer.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Duende.IdentityServer.Hosting;

/// <summary>
/// The backchannel logout request validator
/// </summary>
public interface IBackchannelLogoutRequestValidator
{
    /// <summary>
    /// Validates a back channel logout request.
    /// </summary>
    Task<BackchannelLogoutResult> ValidateAsync(BackchannelLogoutRequest request);
}

/// <summary>
/// Default implementation of the IBackchannelLogoutRequestValidator
/// </summary>
public class DefaultBackchannelLogoutRequestValidator : IBackchannelLogoutRequestValidator
{
    private readonly IOptionsMonitor<OpenIdConnectOptions> _optionsMonitor;
    private readonly ILogger<DefaultBackchannelLogoutRequestValidator> _logger;

    /// <summary>
    /// ctor
    /// </summary>
    public DefaultBackchannelLogoutRequestValidator(IOptionsMonitor<OpenIdConnectOptions> optionsMonitor, ILogger<DefaultBackchannelLogoutRequestValidator> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<BackchannelLogoutResult> ValidateAsync(BackchannelLogoutRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var result = new BackchannelLogoutResult();

        var parameters = await TryGetTokenValidationParameters(request, result);
        if (parameters == null)
        {
            return result;
        }
        
        result.Scheme = request.Scheme;

        var claims = TryValidateLogoutToken(request, result, parameters);
        if (claims == null)
        {
            return result;
        }

        result.Claims = claims;

        return result;
    }


    /// <summary>
    /// Validates the logout token
    /// </summary>
    protected virtual IEnumerable<Claim> TryValidateLogoutToken(BackchannelLogoutRequest request, BackchannelLogoutResult result, TokenValidationParameters parameters)
    {
        var handler = new JsonWebTokenHandler();
        
        var jwtResult = handler.ValidateToken(request.LogoutToken, parameters);
        if (jwtResult == null)
        {
            _logger.LogDebug("No claims in back-channel JWT");
            result.ErrorDescription = "Invalid back-channel logout token";
            return null;
        }

        if (!jwtResult.IsValid)
        {
            _logger.LogDebug($"Error validating logout token. '{jwtResult.Exception.ToString()}'");
            result.ErrorDescription = "Invalid back-channel logout token";
            return null;
        }

        _logger.LogTrace("Claims found in back-channel JWT {claims}", jwtResult.Claims);

        if (jwtResult.ClaimsIdentity.FindFirst(JwtClaimTypes.Subject) == null && jwtResult.ClaimsIdentity.FindFirst(JwtClaimTypes.SessionId) == null)
        {
            result.ErrorDescription = "Logout token missing sub and sid claims.";
            return null;
        }

        var nonce = jwtResult.ClaimsIdentity.FindFirst("nonce")?.Value;
        if (!String.IsNullOrWhiteSpace(nonce))
        {
            result.ErrorDescription = "Logout token should not contain nonce claim.";
            return null;
        }

        var eventsJson = jwtResult.ClaimsIdentity.FindFirst("events")?.Value;
        if (String.IsNullOrWhiteSpace(eventsJson))
        {
            result.ErrorDescription = "Logout token missing events property.";
            return null;
        }

        try
        {
            var events = JsonDocument.Parse(eventsJson);
            if (!events.RootElement.TryGetProperty("http://schemas.openid.net/event/backchannel-logout", out _))
            {
                result.ErrorDescription = "Logout token events property missing http://schemas.openid.net/event/backchannel-logout value.";
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Logout token contains invalid JSON in events claim value. {error}", ex.Message);
            result.ErrorDescription = $"Logout token contains invalid JSON in events claim value.";
            return null;
        }

        return jwtResult.ClaimsIdentity.Claims;
    }

    /// <summary>
    /// Creates the token validation parameters based on the OIDC configuration
    /// </summary>
    protected virtual async Task<TokenValidationParameters> TryGetTokenValidationParameters(BackchannelLogoutRequest request, BackchannelLogoutResult result)
    {
        var scheme = request.Scheme;
        if (scheme == null)
        {
            _logger.LogDebug("Invalid scheme");
            result.ErrorDescription = "Invalid request";
            return null;
        }

        var options = _optionsMonitor.Get(scheme);
        if (options == null)
        {
            _logger.LogDebug("Failed to obtain OpenIdConnectOptions for scheme '{scheme}'", scheme);
            result.ErrorDescription = "Invalid request";
            return null;
        }

        var config = options.Configuration;
        if (config == null)
        {
            config = await options.ConfigurationManager?.GetConfigurationAsync(CancellationToken.None)!;
        }

        if (config == null)
        {
            _logger.LogDebug("Failed to obtain OIDC configuration for scheme '{scheme}'", scheme);
            result.ErrorDescription = "Invalid request";
            return null;
        }

        return new TokenValidationParameters
        {
            ValidIssuer = config.Issuer,
            ValidAudience = options.ClientId,
            IssuerSigningKeys = config.SigningKeys,

            NameClaimType = JwtClaimTypes.Name,
            RoleClaimType = JwtClaimTypes.Role
        };
    }
}

/// <summary>
/// Models the parameters for a back channel logout request.
/// </summary>
public class BackchannelLogoutRequest
{
    /// <summary>
    /// The logout token.
    /// </summary>
    public string LogoutToken { get; set; }

    /// <summary>
    /// The 
    /// </summary>
    public string Scheme { get; internal set; }
}

/// <summary>
/// Models the result of a back channel logout request.
/// </summary>
public class BackchannelLogoutResult
{
    /// <summary>
    /// Indicates if the validation result is an error.
    /// </summary>
    public bool IsError => ErrorDescription.IsPresent();

    /// <summary>
    /// The error during validation.
    /// </summary>
    public string ErrorDescription { get; set; }

    /// <summary>
    /// The IdP authentication scheme used for the user.
    /// </summary>
    public string Scheme { get; set; }

    /// <summary>
    /// The subject id of the user to logout.
    /// </summary>
    public string SubjectId => Claims?.SingleOrDefault(x => x.Type == JwtClaimTypes.Subject)?.Value;
    
    /// <summary>
    /// The session id of the user to logout.
    /// </summary>
    public string SessionId => Claims?.SingleOrDefault(x => x.Type == JwtClaimTypes.SessionId)?.Value;

    /// <summary>
    /// The claims in the logout token.
    /// </summary>
    public IEnumerable<Claim> Claims { get; set; }
}