// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Duende.IdentityServer.Models;
using IdentityModel;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using DynamicClientRegistrationRequest = Duende.IdentityServer.Configuration.Models.DynamicClientRegistration.DynamicClientRegistrationRequest;

namespace Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

public class DynamicClientRegistrationValidator : IDynamicClientRegistrationValidator
{
    private readonly ILogger<DynamicClientRegistrationValidator> _logger;

    public DynamicClientRegistrationValidator(
        ILogger<DynamicClientRegistrationValidator> logger)
    {
        _logger = logger;
    }

    public async Task<DynamicClientRegistrationValidationResult> ValidateAsync(ClaimsPrincipal caller, DynamicClientRegistrationRequest request)
    {
        var client = new Client();

        var result = await SetClientIdAsync(client, request, caller);
        if (result is ValidationStepFailure clientIdStep)
        {
            return clientIdStep.Error;
        }

        result = await SetGrantTypesAsync(client, request, caller);
        if (result is ValidationStepFailure grantTypeValidation)
        {
            return grantTypeValidation.Error;
        }

        result = await SetRedirectUrisAsync(client, request, caller);
        if (result is ValidationStepFailure redirectUrisValidation)
        {
            return redirectUrisValidation.Error;
        }

        result = await SetScopesAsync(client, request, caller);
        if (result is ValidationStepFailure scopeValidation)
        {
            return scopeValidation.Error;
        }

        result = await SetSecretsAsync(client, request, caller);
        if (result is ValidationStepFailure keySetValidation)
        {
            return keySetValidation.Error;
        }

        result = await SetClientNameAsync(client, request, caller);
        if (result is ValidationStepFailure nameValidation)
        {
            return nameValidation.Error;
        }

        result = await SetClientUriAsync(client, request, caller);
        if (result is ValidationStepFailure uriValidation)
        {
            return uriValidation.Error;
        }

        result = await SetMaxAgeAsync(client, request, caller);
        if (result is ValidationStepFailure maxAgeValidation)
        {
            return maxAgeValidation.Error;
        }

        result = await ValidateSoftwareStatementAsync(client, request, caller);
        if (result is ValidationStepFailure softwareStatementValidation)
        {
            return softwareStatementValidation.Error;
        }

        return new DynamicClientRegistrationValidatedRequest(client, request);
    }

    protected virtual Task<ValidationStepResult> SetClientIdAsync(
        Client client,
        DynamicClientRegistrationRequest request,
        ClaimsPrincipal caller)
    {
        client.ClientId = CryptoRandom.CreateUniqueId();
        return ValidationStepSucceeded();
    }

    protected virtual Task<ValidationStepResult> SetGrantTypesAsync(
        Client client,
        DynamicClientRegistrationRequest request,
        ClaimsPrincipal caller)
    {
        if (request.GrantTypes.Count == 0)
        {
            return ValidationStepFailed("grant type is required");
        }

        if (request.GrantTypes.Contains(OidcConstants.GrantTypes.ClientCredentials))
        {
            client.AllowedGrantTypes.Add(GrantType.ClientCredentials);
        }
        if (request.GrantTypes.Contains(OidcConstants.GrantTypes.AuthorizationCode))
        {
            client.AllowedGrantTypes.Add(GrantType.AuthorizationCode);
        }

        // we only support the two above grant types
        if (client.AllowedGrantTypes.Count == 0)
        {
            return ValidationStepFailed("unsupported grant type");
        }

        if (request.GrantTypes.Contains(OidcConstants.GrantTypes.RefreshToken))
        {
            if (client.AllowedGrantTypes.Count == 1 &&
                client.AllowedGrantTypes.FirstOrDefault(t => t.Equals(GrantType.ClientCredentials)) != null)
            {
                return ValidationStepFailed("client credentials does not support refresh tokens");
            }

            client.AllowOfflineAccess = true;
        }

        return ValidationStepSucceeded();
    }

    protected virtual Task<ValidationStepResult> SetRedirectUrisAsync(
        Client client,
        DynamicClientRegistrationRequest request,
        ClaimsPrincipal caller)
    {
        if (client.AllowedGrantTypes.Contains(GrantType.AuthorizationCode))
        {
            if (request.RedirectUris.Any())
            {
                foreach (var requestRedirectUri in request.RedirectUris)
                {
                    if (requestRedirectUri.IsAbsoluteUri)
                    {
                        client.RedirectUris.Add(requestRedirectUri.AbsoluteUri);
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

        if (client.AllowedGrantTypes.Count == 1 &&
            client.AllowedGrantTypes.FirstOrDefault(t => t.Equals(GrantType.ClientCredentials)) != null)
        {
            if (request.RedirectUris.Any())
            {
                return ValidationStepFailed("redirect URI not compatible with client_credentials grant type", DynamicClientRegistrationErrors.InvalidRedirectUri);
            }
        }

        return ValidationStepSucceeded();
    }

    protected virtual Task<ValidationStepResult> SetScopesAsync(
        Client client,
        DynamicClientRegistrationRequest request,
        ClaimsPrincipal caller)
    {
        if (string.IsNullOrEmpty(request.Scope))
        {
            return SetDefaultScopes(client, request, caller);
        }
        else
        {
            var scopes = request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (scopes.Contains("offline_access"))
            {
                scopes = scopes.Where(s => s != "offline_access").ToArray();
                _logger.LogDebug("offline_access should not be passed as a scope to dynamic client registration. Use the refresh_token grant_type instead.");
            }

            foreach (var scope in scopes)
            {
                client.AllowedScopes.Add(scope);
            }
        }
        return ValidationStepSucceeded();
    }

    protected virtual Task<ValidationStepResult> SetDefaultScopes(
        Client client,
        DynamicClientRegistrationRequest request,
        ClaimsPrincipal caller)
    {
        // This default implementation sets no scopes and is intended as an extension point.
        _logger.LogDebug("No scopes requested for dynamic client registration, and no default scope behavior implemented. To set default scopes, extend the DynamicClientRegistrationValidator and override the SetDefaultScopes method.");
        return ValidationStepSucceeded();
    }

    protected virtual Task<ValidationStepResult> SetSecretsAsync(
        Client client,
        DynamicClientRegistrationRequest request,
        ClaimsPrincipal caller)
    {
        if (request.JwksUri is not null && request.Jwks is not null)
        {
            return ValidationStepFailed("The jwks_uri and jwks parameters must not be used together");
        }

        if (request.Jwks is null && request.TokenEndpointAuthenticationMethod == OidcConstants.EndpointAuthenticationMethods.PrivateKeyJwt)
        {
            return ValidationStepFailed("Missing jwks parameter - the private_key_jwt token_endpoint_auth_method requires the jwks parameter");
        }

        if (request.Jwks is not null && request.TokenEndpointAuthenticationMethod != OidcConstants.EndpointAuthenticationMethods.PrivateKeyJwt)
        {
            return ValidationStepFailed("Invalid authentication method - the jwks parameter requires the private_key_jwt token_endpoint_auth_method");
        }

        if (request.Jwks?.Keys is not null)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
            };

            foreach (var key in request.Jwks.Keys)
            {
                var jwk = JsonSerializer.Serialize(key, jsonOptions);

                // We parse the jwk to ensure it is valid, but we utlimately
                // write the original text that was passed to us (parsing can
                // change it)
                try
                {
                    var parsedJwk = new IdentityModel.Jwk.JsonWebKey(jwk);

                    // TODO - Other HMAC hashing algorithms would also expect a private key
                    if (parsedJwk.HasPrivateKey && parsedJwk.Alg != SecurityAlgorithms.HmacSha256)
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

                client.ClientSecrets.Add(new Secret
                {
                    // TODO - Define this constant
                    Type = "JWK", //IdentityServerConstants.SecretTypes.JsonWebKey,
                    Value = jwk
                });
            }
        }
        return ValidationStepSucceeded();
    }


    protected virtual Task<ValidationStepResult> SetClientNameAsync(
        Client client,
        DynamicClientRegistrationRequest request,
        ClaimsPrincipal caller)
    {
        if (!string.IsNullOrWhiteSpace(request.ClientName))
        {
            client.ClientName = request.ClientName;
        }
        return ValidationStepSucceeded();
    }

    protected virtual Task<ValidationStepResult> SetClientUriAsync(
        Client client,
        DynamicClientRegistrationRequest request,
        ClaimsPrincipal caller)
    {
        if (request.ClientUri != null)
        {
            client.ClientUri = request.ClientUri.AbsoluteUri;
        }
        return ValidationStepSucceeded();
    }

    protected virtual Task<ValidationStepResult> SetMaxAgeAsync(
        Client client,
        DynamicClientRegistrationRequest request,
        ClaimsPrincipal caller)
    {
        if (request.DefaultMaxAge.HasValue)
        {
            if (request.DefaultMaxAge <= 0)
            {
                return ValidationStepFailed("default_max_age must be greater than 0 if used");
            }
            client.UserSsoLifetime = request.DefaultMaxAge;
        }
        return ValidationStepSucceeded();
    }

    protected virtual Task<ValidationStepResult> ValidateSoftwareStatementAsync(
        Client client,
        DynamicClientRegistrationRequest request,
        ClaimsPrincipal caller)
    {
        return ValidationStepSucceeded();
    }

    protected Task<ValidationStepResult> ValidationStepFailed(string errorDescription,
        string error = DynamicClientRegistrationErrors.InvalidClientMetadata) =>
            Task.FromResult<ValidationStepResult>(new ValidationStepFailure(
                    error,
                    errorDescription
                ));

    protected Task<ValidationStepResult> ValidationStepSucceeded() =>
        Task.FromResult<ValidationStepResult>(new ValidationStepSuccess());

    protected abstract class ValidationStepResult { }

    protected class ValidationStepFailure : ValidationStepResult
    {
        public DynamicClientRegistrationValidationError Error { get; set; }

        public ValidationStepFailure(string error, string errorDescription)
            : this(new DynamicClientRegistrationValidationError(error, errorDescription))
        {
        }

        public ValidationStepFailure(DynamicClientRegistrationValidationError error)
        {
            Error = error;
        }
    }

    protected class ValidationStepSuccess : ValidationStepResult { }
}