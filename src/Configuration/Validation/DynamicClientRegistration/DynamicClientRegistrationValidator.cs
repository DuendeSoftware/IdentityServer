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

        result = await SetClientUriAsync(context);
        if (result is ValidationStepFailure uriValidation)
        {
            return uriValidation.Error;
        }

        result = await SetMaxAgeAsync(context);
        if (result is ValidationStepFailure maxAgeValidation)
        {
            return maxAgeValidation.Error;
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
                // TODO - When we implement PAR, this may no longer be an error for clients that use PAR
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

        if (context.Request.Jwks?.Keys is not null)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
            };

            foreach (var key in context.Request.Jwks.Keys)
            {
                var jwk = JsonSerializer.Serialize(key, jsonOptions);

                // We parse the jwk to ensure it is valid, but we utlimately
                // write the original text that was passed to us (parsing can
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
                catch (InvalidOperationException)
                {
                    return ValidationStepFailed("malformed jwk");
                }
                catch (JsonException)
                {
                    return ValidationStepFailed("malformed jwk");
                }

                context.Client.ClientSecrets.Add(new Secret
                {
                    // TODO - Define this constant
                    Type = "JWK", //IdentityServerConstants.SecretTypes.JsonWebKey,
                    Value = jwk
                });
            }
        }
        return ValidationStepSucceeded();
    }

    /// <summary>
    /// Validates the requested client name uses it to set the redirect uris of
    /// the client.
    /// </summary>
    /// <param name="context">The validation context, which includes the client
    /// model that will have its name set, the DCR request, and other contextual
    /// information.
    /// </param>
    /// <returns>A task that returns a <see cref="ValidationStepResult"/>, which
    /// either represents that this step succeeded or failed.</returns>
    protected virtual Task<ValidationStepResult> SetClientNameAsync(DynamicClientRegistrationValidationContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.Request.ClientName))
        {
            context.Client.ClientName = context.Request.ClientName;
        }
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
        if (context.Request.ClientUri != null)
        {
            context.Client.ClientUri = context.Request.ClientUri.AbsoluteUri;
        }
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
            if (context.Request.DefaultMaxAge <= 0)
            {
                return ValidationStepFailed("default_max_age must be greater than 0 if used");
            }
            context.Client.UserSsoLifetime = context.Request.DefaultMaxAge;
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

    // TODO Add a method for validating things like logo uri and other stuff
    // that isn't really controlling the protocols, just describes the client.

    private static Task<ValidationStepResult> ValidationStepFailed(string errorDescription,
        string error = DynamicClientRegistrationErrors.InvalidClientMetadata) =>
            Task.FromResult<ValidationStepResult>(new ValidationStepFailure(
                    error,
                    errorDescription
                ));

    private static Task<ValidationStepResult> ValidationStepSucceeded() =>
        Task.FromResult(ValidationStepResult.Success);
}