// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Duende.IdentityServer.Models;
using IdentityModel;
using Microsoft.Extensions.Logging;
using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;

namespace Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

/// <inheritdoc/>
public class DynamicClientRegistrationValidator : IDynamicClientRegistrationValidator
{
    private readonly ILogger<DynamicClientRegistrationValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicClientRegistrationValidator"/> class.
    /// </summary>
    public DynamicClientRegistrationValidator(
        ILogger<DynamicClientRegistrationValidator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<DynamicClientRegistrationValidationResult> ValidateAsync(DynamicClientRegistrationRequest request, ClaimsPrincipal caller)
    {
        var context = new DynamicClientRegistrationValidationContext(request, caller);

        var result = await ValidateSoftwareStatementAsync(context);
        if (result is ValidationStepFailure softwareStatementValidation)
        {
            return softwareStatementValidation.Error;
        }

        result = await SetGrantTypesAsync(context);
        if (result is ValidationStepFailure grantTypeValidation)
        {
            return grantTypeValidation.Error;
        }

        result = await SetRedirectUrisAsync(context);
        if (result is ValidationStepFailure redirectUrisValidation)
        {
            return redirectUrisValidation.Error;
        }

        result = await SetScopesAsync(context);
        if (result is ValidationStepFailure scopeValidation)
        {
            return scopeValidation.Error;
        }

        result = await SetSecretsAsync(context);
        if (result is ValidationStepFailure keySetValidation)
        {
            return keySetValidation.Error;
        }

        result = await SetClientNameAsync(context);
        if (result is ValidationStepFailure nameValidation)
        {
            return nameValidation.Error;
        }

        result = await SetLogoutParametersAsync(context);
        if(result is ValidationStepFailure logoutValidation)
        {
            return logoutValidation.Error;
        }

        result = await SetMaxAgeAsync(context);
        if (result is ValidationStepFailure maxAgeValidation)
        {
            return maxAgeValidation.Error;
        }

        result = await SetUserInterfaceProperties(context);
        if (result is ValidationStepFailure miscValidation)
        {
            return miscValidation.Error;
        }

        result = await SetPublicClientProperties(context);
        if (result is ValidationStepFailure publicClientValidation)
        {
            return publicClientValidation.Error;
        }

        result = await SetAccessTokenProperties(context);
        if (result is ValidationStepFailure accessTokenValidation)
        {
            return accessTokenValidation.Error;
        }

        result = await SetIdTokenProperties(context);
        if (result is ValidationStepFailure idTokenValidation)
        {
            return idTokenValidation.Error;
        }

        result = await SetServerSideSessionProperties(context);
        if (result is ValidationStepFailure serverSideSessionValidation)
        {
            return serverSideSessionValidation.Error;
        }

        return new DynamicClientRegistrationValidatedRequest(context.Client, request);
    }

    /// <summary>
    /// Validates requested grant types and uses them to set the allowed grant
    /// types of the client.
    /// </summary>
    /// <param name="context">The validation context, which includes the client
    /// model that will have its allowed grant types set, the DCR request, and
    /// other contextual information.
    /// </param>
    /// <returns>A task that returns a <see cref="ValidationStepResult"/>, which
    /// either represents that this step succeeded or failed.</returns>
    protected virtual Task<ValidationStepResult> SetGrantTypesAsync(DynamicClientRegistrationValidationContext context)
    {
        if (context.Request.GrantTypes.Count == 0)
        {
            return ValidationStepFailed("grant type is required");
        }

        if (context.Request.GrantTypes.Contains(OidcConstants.GrantTypes.ClientCredentials))
        {
            context.Client.AllowedGrantTypes.Add(GrantType.ClientCredentials);
        }
        if (context.Request.GrantTypes.Contains(OidcConstants.GrantTypes.AuthorizationCode))
        {
            context.Client.AllowedGrantTypes.Add(GrantType.AuthorizationCode);
            if(context.Request.AuthorizationCodeLifetime.HasValue)
            {
                var lifetime = context.Request.AuthorizationCodeLifetime.Value;
                if (lifetime <= 0)
                {
                    return ValidationStepFailed("The authorization code lifetime must be greater than 0 if used");
                }
                context.Client.AuthorizationCodeLifetime = lifetime;
            }
        }

        // we only support the two above grant types
        if (context.Client.AllowedGrantTypes.Count == 0)
        {
            return ValidationStepFailed("unsupported grant type");
        }

        if (context.Request.GrantTypes.Contains(OidcConstants.GrantTypes.RefreshToken))
        {
            // TODO - make it more explicit which grant types support refresh
            // tokens (make this a positive check, for robustness).
            if (context.Client.AllowedGrantTypes.Count == 1 &&
                context.Client.AllowedGrantTypes.FirstOrDefault(t => t.Equals(GrantType.ClientCredentials)) != null)
            {
                return ValidationStepFailed("client credentials does not support refresh tokens");
            }

            context.Client.AllowOfflineAccess = true;
            if (context.Request.RefreshTokenExpiration != null)
            {
                if (!Enum.TryParse<TokenExpiration>(context.Request.RefreshTokenExpiration, out var tokenExpiration))
                {
                    return ValidationStepFailed("invalid refresh token expiration - use 'absolute' or 'sliding'");
                }
                context.Client.RefreshTokenExpiration = tokenExpiration;
            }
            if (context.Request.SlidingRefreshTokenLifetime.HasValue)
            {
                var lifetime = context.Request.SlidingRefreshTokenLifetime.Value;
                if (lifetime <= 0)
                {
                    return ValidationStepFailed("The sliding refresh token lifetime must be greater than 0 if used");
                }
                context.Client.SlidingRefreshTokenLifetime = lifetime;
            }
            if (context.Request.AbsoluteRefreshTokenLifetime.HasValue)
            {
                var lifetime = context.Request.AbsoluteRefreshTokenLifetime.Value;
                if (lifetime <= 0)
                {
                    return ValidationStepFailed("The absolute refresh token lifetime must be greater than 0 if used");
                }
                context.Client.AbsoluteRefreshTokenLifetime = lifetime;                
            }
            if (context.Request.RefreshTokenUsage != null)
            {
                if (!Enum.TryParse<TokenUsage>(context.Request.RefreshTokenUsage, out var tokenUsage))
                {
                    return ValidationStepFailed("invalid refresh token usage - use 'OneTimeOnly' or 'ReUse'");
                }
                context.Client.RefreshTokenUsage = tokenUsage;
            }
            if (context.Request.UpdateAccessTokenClaimsOnRefresh.HasValue)
            {
                context.Client.UpdateAccessTokenClaimsOnRefresh = context.Request.UpdateAccessTokenClaimsOnRefresh.Value;
            }
        }

        return ValidationStepSucceeded();
    }

    /// <summary>
    /// Validates requested redirect uris and uses them to set the redirect uris
    /// of the client.
    /// </summary>
    /// <param name="context">The validation context, which includes the client
    /// model that will have its redirect uri set, the DCR request, and other
    /// contextual information.
    /// </param>
    /// <returns>A task that returns a <see cref="ValidationStepResult"/>, which
    /// either represents that this step succeeded or failed.</returns>
    protected virtual Task<ValidationStepResult> SetRedirectUrisAsync(DynamicClientRegistrationValidationContext context)
    {
        if (context.Client.AllowedGrantTypes.Contains(GrantType.AuthorizationCode))
        {
            if (context.Request.RedirectUris.Any())
            {
                foreach (var requestRedirectUri in context.Request.RedirectUris)
                {
                    if (requestRedirectUri.IsAbsoluteUri)
                    {
                        context.Client.RedirectUris.Add(requestRedirectUri.AbsoluteUri);
                    }
                    else
                    {
                        return ValidationStepFailed("malformed redirect URI", DynamicClientRegistrationErrors.InvalidRedirectUri);
                    }
                }
            }
            else
            {
                // Note that when we implement PAR, this may no longer be an error for clients that use PAR
                return ValidationStepFailed("redirect URI required for authorization_code grant type", DynamicClientRegistrationErrors.InvalidRedirectUri);
            }
        }

        if (context.Client.AllowedGrantTypes.Count == 1 &&
            context.Client.AllowedGrantTypes.FirstOrDefault(t => t.Equals(GrantType.ClientCredentials)) != null)
        {
            if (context.Request.RedirectUris.Any())
            {
                return ValidationStepFailed("redirect URI not compatible with client_credentials grant type", DynamicClientRegistrationErrors.InvalidRedirectUri);
            }
        }

        return ValidationStepSucceeded();
    }

    /// <summary>
    /// Validates requested scopes and uses them to set the scopes of the
    /// client.
    /// </summary>
    /// <param name="context">The validation context, which includes the client
    /// model that will have its scopes set, the DCR request, and other
    /// contextual information.
    /// </param>
    /// <returns>A task that returns a <see cref="ValidationStepResult"/>, which
    /// either represents that this step succeeded or failed.</returns>
    protected virtual Task<ValidationStepResult> SetScopesAsync(DynamicClientRegistrationValidationContext context)
    {
        if (string.IsNullOrEmpty(context.Request.Scope))
        {
            return SetDefaultScopes(context);
        }
        else
        {
            var scopes = context.Request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (scopes.Contains("offline_access"))
            {
                scopes = scopes.Where(s => s != "offline_access").ToArray();
                _logger.LogDebug("offline_access should not be passed as a scope to dynamic client registration. Use the refresh_token grant_type instead.");
            }

            foreach (var scope in scopes)
            {
                context.Client.AllowedScopes.Add(scope);
            }
        }
        return ValidationStepSucceeded();
    }

    /// <summary>
    /// Sets scopes on the client when no scopes are requested. This default
    /// implementation sets no scopes and is intended as an extension point.
    /// </summary>
    /// <param name="context">The validation context, which includes the client
    /// model that will have its scopes set, the DCR request, and other
    /// contextual information.
    /// </param>
    /// <returns>A task that returns a <see cref="ValidationStepResult"/>, which
    /// either represents that this step succeeded or failed.</returns>
    protected virtual Task<ValidationStepResult> SetDefaultScopes(DynamicClientRegistrationValidationContext context)
    {
        _logger.LogDebug("No scopes requested for dynamic client registration, and no default scope behavior implemented. To set default scopes, extend the DynamicClientRegistrationValidator and override the SetDefaultScopes method.");
        return ValidationStepSucceeded();
    }

    /// <summary>
    /// Validates the requested jwks to set the secrets of the client.  
    /// </summary>
    /// <param name="context">The validation context, which includes the client
    /// model that will have its secrets set, the DCR request, and other
    /// contextual information.
    /// </param>
    /// <returns>A task that returns a <see cref="ValidationStepResult"/>, which
    /// either represents that this step succeeded or failed.</returns>
    protected virtual Task<ValidationStepResult> SetSecretsAsync(DynamicClientRegistrationValidationContext context)
    {
        if (context.Request.JwksUri is not null && context.Request.Jwks is not null)
        {
            return ValidationStepFailed("The jwks_uri and jwks parameters must not be used together");
        }

        if (context.Request.Jwks is null && context.Request.TokenEndpointAuthenticationMethod == OidcConstants.EndpointAuthenticationMethods.PrivateKeyJwt)
        {
            return ValidationStepFailed("Missing jwks parameter - the private_key_jwt token_endpoint_auth_method requires the jwks parameter");

        }
        if (context.Request.Jwks is not null && context.Request.TokenEndpointAuthenticationMethod != OidcConstants.EndpointAuthenticationMethods.PrivateKeyJwt)
        {
            return ValidationStepFailed("Invalid authentication method - the jwks parameter requires the private_key_jwt token_endpoint_auth_method");
        }

        if (context.Request.Jwks?.Keys is null && context.Request.RequireSignedRequestObject == true)
        {
            return ValidationStepFailed("Jwks are required when the require signed request object flag is enabled");
        }

        if (context.Request.Jwks?.Keys is not null)
        {
            context.Client.RequireRequestObject = context.Request.RequireSignedRequestObject ?? false;

            var jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
            };

            foreach (var key in context.Request.Jwks.Keys)
            {
                var jwk = JsonSerializer.Serialize(key, jsonOptions);

                // We parse the jwk to ensure it is valid, but we ultimately
                // write the original string that was passed to us (parsing can
                // change it)
                try
                {
                    var parsedJwk = new IdentityModel.Jwk.JsonWebKey(jwk);

                    // TODO - Other HMAC hashing algorithms would also expect a private key
                    if (parsedJwk.HasPrivateKey && parsedJwk.Alg != "HS256")
                    {
                        return ValidationStepFailed("unexpected private key in jwk");
                    }
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "Failed to parse jwk");
                    return ValidationStepFailed("malformed jwk");
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse jwk");
                    return ValidationStepFailed("malformed jwk");
                }

                context.Client.ClientSecrets.Add(new Secret
                {
                    Type = "JWK",
                    Value = jwk
                });
            }
        }
        return ValidationStepSucceeded();
    }

    /// <summary>
    /// Validates the requested client name uses it to set the name of the
    /// client.
    /// </summary>
    /// <param name="context">The validation context, which includes the client
    /// model that will have its name set, the DCR request, and other contextual
    /// information.
    /// </param>
    /// <returns>A task that returns a <see cref="ValidationStepResult"/>, which
    /// either represents that this step succeeded or failed.</returns>
    protected virtual Task<ValidationStepResult> SetClientNameAsync(DynamicClientRegistrationValidationContext context)
    {
        context.Client.ClientName = context.Request?.ClientName;
        return ValidationStepSucceeded();
    }

    /// <summary>
    /// Validates the requested client uri and uses it to set the client uri of
    /// the client.
    /// </summary>
    /// <param name="context">The validation context, which includes the client
    /// model that will have its client uri set, the DCR request, and other
    /// contextual information.
    /// </param>
    /// <returns>A task that returns a <see cref="ValidationStepResult"/>, which
    /// either represents that this step succeeded or failed.</returns>
    protected virtual Task<ValidationStepResult> SetClientUriAsync(DynamicClientRegistrationValidationContext context)
    {
        return ValidationStepSucceeded();
    }

    /// <summary>
    /// Validates the requested client parameters related to logout and uses
    /// them to set the corresponding properties in the client. Those parameters
    /// include the post logout redirect uris, front channel and back channel
    /// uris, and flags for the front and back channel uris indicating if they
    /// require session ids.
    /// </summary>
    /// <param name="context">The validation context, which includes the client
    /// model that will have its logout parameters set, the DCR request, and
    /// other contextual information.
    /// </param>
    /// <returns>A task that returns a <see cref="ValidationStepResult"/>, which
    /// either represents that this step succeeded or failed.</returns>
    protected virtual Task<ValidationStepResult> SetLogoutParametersAsync(DynamicClientRegistrationValidationContext context)
    {
        context.Client.PostLogoutRedirectUris = context.Request.PostLogoutRedirectUris.Select(uri => uri.ToString()).ToList();
        context.Client.FrontChannelLogoutUri = context.Request.FrontChannelLogoutUri?.AbsoluteUri;
        context.Client.FrontChannelLogoutSessionRequired = context.Request.FrontChannelLogoutSessionRequired ?? false;
        context.Client.BackChannelLogoutUri = context.Request.BackChannelLogoutUri?.AbsoluteUri;
        context.Client.BackChannelLogoutSessionRequired = context.Request.BackchannelLogoutSessionRequired ?? false;

        return ValidationStepSucceeded();
    }

    /// <summary>
    /// Validates the requested default max age and uses it to set the user sso
    /// lifetime of the client.
    /// </summary>
    /// <param name="context">The validation context, which includes the client
    /// model that will have its max age set, the DCR request, and other
    /// contextual information.
    /// </param>
    /// <returns>A task that returns a <see cref="ValidationStepResult"/>, which
    /// either represents that this step succeeded or failed.</returns>
    protected virtual Task<ValidationStepResult> SetMaxAgeAsync(DynamicClientRegistrationValidationContext context)
    {
        if (context.Request.DefaultMaxAge.HasValue)
        {
            var lifetime = context.Request.DefaultMaxAge;
            if (lifetime <= 0)
            {
                return ValidationStepFailed("default_max_age must be greater than 0 if used");
            }
            context.Client.UserSsoLifetime = lifetime;
        }
        return ValidationStepSucceeded();
    }

    /// <summary>
    /// Validates the software statement of the request. This default
    /// implementation does nothing, and is included as an extension point.
    /// </summary>
    /// <param name="context">The validation context, which includes the client
    /// model that is being built up, the DCR request, and other contextual
    /// information.</param>
    /// <returns>A task that returns a <see cref="ValidationStepResult"/>, which
    /// either represents that this step succeeded or failed.</returns>
    protected virtual Task<ValidationStepResult> ValidateSoftwareStatementAsync(DynamicClientRegistrationValidationContext context)
    {
        return ValidationStepSucceeded();
    }

    /// <summary>
    /// Validates the requested client parameters related to public clients and
    /// uses them to set the corresponding properties in the client. Those
    /// parameters include the allow access tokens via browser flag, the require
    /// client secret flag, and the allowed cors origins.
    /// </summary>
    /// <param name="context">The validation context, which includes the client
    /// model that will have its public client properties set, the DCR request,
    /// and other contextual information.
    /// </param>
    /// <returns>A task that returns a <see cref="ValidationStepResult"/>, which
    /// either represents that this step succeeded or failed.</returns>
    protected virtual Task<ValidationStepResult> SetPublicClientProperties(DynamicClientRegistrationValidationContext context)
    {
        context.Client.AllowedCorsOrigins = context.Request.AllowedCorsOrigins;
        if (context.Request.RequireClientSecret.HasValue)
        {
            context.Client.RequireClientSecret = context.Request.RequireClientSecret.Value;
        }
        return ValidationStepSucceeded();
    }

    /// <summary>
    /// Validates the requested client parameters related to access tokens and
    /// uses them to set the corresponding properties in the client. Those
    /// parameters include the allow access token type and access token lifetime.
    /// </summary>
    /// <param name="context">The validation context, which includes the client
    /// model that will have its access token properties set, the DCR request,
    /// and other contextual information.
    /// </param>
    /// <returns>A task that returns a <see cref="ValidationStepResult"/>, which
    /// either represents that this step succeeded or failed.</returns>
    protected virtual Task<ValidationStepResult> SetAccessTokenProperties(DynamicClientRegistrationValidationContext context)
    {
        if (context.Request.AccessTokenType != null)
        {
            if (!Enum.TryParse<AccessTokenType>(context.Request.AccessTokenType, out var tokenType))
            {
                return ValidationStepFailed("invalid access token type - use 'jwt' or 'reference'");
            }
            context.Client.AccessTokenType = tokenType;
        }
        if(context.Request.AccessTokenLifetime.HasValue)
        {
            var lifetime = context.Request.AccessTokenLifetime.Value;
            if (lifetime <= 0)
            {
                return ValidationStepFailed("The access token lifetime must be greater than 0 if used");
            }
            context.Client.AccessTokenLifetime = lifetime;
        }
        return ValidationStepSucceeded();
    }

    /// <summary>
    /// Validates the requested client parameters related to id tokens and uses
    /// them to set the corresponding properties in the client. Those parameters
    /// include the id token lifetime and the allowed id token signing
    /// algorithms.
    /// </summary>
    /// <param name="context">The validation context, which includes the client
    /// model that will have its id token properties set, the DCR request, and
    /// other contextual information.
    /// </param>
    /// <returns>A task that returns a <see cref="ValidationStepResult"/>, which
    /// either represents that this step succeeded or failed.</returns>
    protected virtual Task<ValidationStepResult> SetIdTokenProperties(DynamicClientRegistrationValidationContext context)
    {
        if(context.Request.IdentityTokenLifetime.HasValue)
        {
            var lifetime = context.Request.IdentityTokenLifetime.Value;
            if (lifetime <= 0)
            {
                return ValidationStepFailed("The identity token lifetime must be greater than 0 if used");
            }
            context.Client.IdentityTokenLifetime = lifetime;
        }
        context.Client.AllowedIdentityTokenSigningAlgorithms = context.Request.AllowedIdentityTokenSigningAlgorithms;
        return ValidationStepSucceeded();
    }

    /// <summary>
    /// Validates the requested client parameters related to server side
    /// sessions and uses them to set the corresponding properties in the
    /// client. Those parameters include the coordinate lifetime with user
    /// session flag.
    /// </summary>
    /// <param name="context">The validation context, which includes the client
    /// model that will have its server side session properties set, the DCR request,
    /// and other contextual information.
    /// </param>
    /// <returns>A task that returns a <see cref="ValidationStepResult"/>, which
    /// either represents that this step succeeded or failed.</returns>
    protected virtual Task<ValidationStepResult> SetServerSideSessionProperties(DynamicClientRegistrationValidationContext context)
    {
        if(context.Request.CoordinateLifetimeWithUserSession.HasValue)
        {
            context.Client.CoordinateLifetimeWithUserSession = context.Request.CoordinateLifetimeWithUserSession.Value;
        }
        return ValidationStepSucceeded();
    }

    /// <summary>
    /// Validates details of the request that control the user interface,
    /// including the logo uri, client uri, initiate login uri, enable local
    /// login flag, and identity provider restrictions, and uses them to set the
    /// corresponding client properties. 
    /// </summary>
    /// <param name="context">The validation context, which includes the client
    /// model that will have miscellaneous properties set, the DCR request, and
    /// other contextual information.</param>
    /// <returns>A task that returns a <see cref="ValidationStepResult"/>, which
    /// either represents that this step succeeded or failed.</returns>
    protected virtual Task<ValidationStepResult> SetUserInterfaceProperties(DynamicClientRegistrationValidationContext context)
    {
        // Misc Uris
        context.Client.LogoUri = context.Request.LogoUri?.ToString();
        context.Client.InitiateLoginUri = context.Request.InitiateLoginUri?.ToString();
        
        // Login Providers
        if(context.Request.EnableLocalLogin.HasValue)
        {
            context.Client.EnableLocalLogin = context.Request.EnableLocalLogin.Value;
        }
        context.Client.IdentityProviderRestrictions = context.Request.IdentityProviderRestrictions;
        if(context.Request.RequireConsent.HasValue)
        {
            context.Client.RequireConsent = context.Request.RequireConsent.Value;
        }

        // Consent
        context.Client.ClientUri = context.Request.ClientUri?.AbsoluteUri;
        if(context.Request.AllowRememberConsent.HasValue)
        {
            context.Client.AllowRememberConsent = context.Request.AllowRememberConsent.Value;
        }
        if(context.Request.ConsentLifetime.HasValue)
        {
            var lifetime = context.Request.ConsentLifetime.Value;
            if (lifetime <= 0)
            {
                return ValidationStepFailed("The consent lifetime must be greater than 0 if used");
            }
            context.Client.ConsentLifetime = lifetime;
        }

        return ValidationStepSucceeded();
    }

    private static Task<ValidationStepResult> ValidationStepFailed(string errorDescription,
        string error = DynamicClientRegistrationErrors.InvalidClientMetadata) =>
            Task.FromResult<ValidationStepResult>(new ValidationStepFailure(
                    error,
                    errorDescription
                ));

    private static Task<ValidationStepResult> ValidationStepSucceeded() =>
        Task.FromResult(ValidationStepResult.Success);
}