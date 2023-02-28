using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Models;
using IdentityModel;
using IdentityModel.Client;
using IdentityModel.Jwk;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using DynamicClientRegistrationRequest = Duende.IdentityServer.Configuration.Models.DynamicClientRegistration.DynamicClientRegistrationRequest;

namespace Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

public class DefaultDynamicClientRegistrationValidator : IDynamicClientRegistrationValidator
{
    private readonly DiscoveryCache _discoveryCache;
    private readonly ILogger<DefaultDynamicClientRegistrationValidator> _logger;

    public DefaultDynamicClientRegistrationValidator(
        ILogger<DefaultDynamicClientRegistrationValidator> logger, 
        DiscoveryCache discoveryCache)
    {
        _logger = logger;
        _discoveryCache = discoveryCache;
    }

    // TODO - Add log messages throughout
    public async Task<DynamicClientRegistrationValidationResult> ValidateAsync(ClaimsPrincipal caller, DynamicClientRegistrationRequest request)
    {
        var client = new Client
        {
            // TODO - make this an extension point
            ClientId = CryptoRandom.CreateUniqueId()
        };

        //////////////////////////////
        // validate grant types
        //////////////////////////////

        if (request.GrantTypes.Count == 0)
        {
            return new DynamicClientRegistrationValidationError(
                DynamicClientRegistrationErrors.InvalidClientMetadata,
                "grant type is required");
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
            return new DynamicClientRegistrationValidationError(
                DynamicClientRegistrationErrors.InvalidClientMetadata,
                "unsupported grant type");
        }

        if (request.GrantTypes.Contains(OidcConstants.GrantTypes.RefreshToken))
        {
            if (client.AllowedGrantTypes.Count == 1 &&
                client.AllowedGrantTypes.FirstOrDefault(t => t.Equals(GrantType.ClientCredentials)) != null)
            {
                return new DynamicClientRegistrationValidationError(
                    DynamicClientRegistrationErrors.InvalidClientMetadata,
                     "client credentials does not support refresh tokens");
            }

            client.AllowOfflineAccess = true;
        }

        //////////////////////////////
        // validate redirect URIs
        //////////////////////////////
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
                        return new DynamicClientRegistrationValidationError(
                            DynamicClientRegistrationErrors.InvalidRedirectUri,
                            "malformed redirect URI");
                    }
                }
            }
            else
            {
                return new DynamicClientRegistrationValidationError(
                    DynamicClientRegistrationErrors.InvalidRedirectUri,
                    "redirect URI required for authorization_code grant type");
            }
        }

        if (client.AllowedGrantTypes.Count == 1 &&
            client.AllowedGrantTypes.FirstOrDefault(t => t.Equals(GrantType.ClientCredentials)) != null)
        {
            if (request.RedirectUris.Any())
            {
                return new DynamicClientRegistrationValidationError(
                    DynamicClientRegistrationErrors.InvalidRedirectUri,
                    "redirect URI not compatible with client_credentials grant type");
            }
        }

        //////////////////////////////
        // validate scopes
        //////////////////////////////
        if (string.IsNullOrEmpty(request.Scope))
        {
            // todo: what to do when scopes are missing? error - leave up to custom validator?
        }
        else
        {
            var scopes = request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Review: How should we handle a request with the offline_access scope?
            // How does that interact with the grant_type param?
            //
            // Proposal:
            // if (grant_types includes refresh_token )
            //       offline_access scope is optional
            // else
            //       offline_access scope is forbidden

            var discovery = await _discoveryCache.GetAsync();
            if(discovery.IsError) 
            {
                throw new Exception("Discovery failed");
            }
            var unsupportedScopes = scopes.Except(discovery.ScopesSupported);
            if(unsupportedScopes.Any())
            {
                var unsupportedScopeNames = string.Join(" ", unsupportedScopes);
                return new DynamicClientRegistrationValidationError(
                    DynamicClientRegistrationErrors.InvalidClientMetadata,
                    $"unsupported scope: {unsupportedScopeNames}"
                );
            }

            foreach (var scope in scopes)
            {
                client.AllowedScopes.Add(scope);
            }
        }

        //////////////////////////////
        // secret handling
        //////////////////////////////

        if (request.JwksUri is not null && request.Jwks is not null)
        {
            return new DynamicClientRegistrationValidationError(
                DynamicClientRegistrationErrors.InvalidClientMetadata,
                "The jwks_uri and jwks parameters must not be used together.");
        }

        if (request.Jwks is null && request.TokenEndpointAuthenticationMethod == OidcConstants.EndpointAuthenticationMethods.PrivateKeyJwt)
        {
            return new DynamicClientRegistrationValidationError(
                DynamicClientRegistrationErrors.InvalidClientMetadata,
                "Missing jwks parameter - the private_key_jwt token_endpoint_auth_method requires the jwks parameter");
        }

        if (request.Jwks is not null && request.TokenEndpointAuthenticationMethod != OidcConstants.EndpointAuthenticationMethods.PrivateKeyJwt)
        {
            return new DynamicClientRegistrationValidationError(
                DynamicClientRegistrationErrors.InvalidClientMetadata,
                "Invalid authentication method - the jwks parameter requires the private_key_jwt token_endpoint_auth_method");
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
                        return new DynamicClientRegistrationValidationError(
                            DynamicClientRegistrationErrors.InvalidClientMetadata,
                            "unexpected private key in jwk"
                        );
                    }
                }
                catch (InvalidOperationException)
                {
                    return new DynamicClientRegistrationValidationError(
                        DynamicClientRegistrationErrors.InvalidClientMetadata,
                        "malformed jwk"
                    );
                }
                catch (JsonException)
                {
                    return new DynamicClientRegistrationValidationError(
                        DynamicClientRegistrationErrors.InvalidClientMetadata,
                        "malformed jwk"
                    );
                }

                client.ClientSecrets.Add(new Secret
                {
                    Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                    Value = jwk
                });
            }
        }


        //////////////////////////////
        // misc
        //////////////////////////////
        if (!string.IsNullOrWhiteSpace(request.ClientName))
        {
            client.ClientName = request.ClientName;
        }

        if (request.ClientUri != null)
        {
            client.ClientUri = request.ClientUri.AbsoluteUri;
        }

        if (request.DefaultMaxAge.HasValue)
        {
            client.UserSsoLifetime = request.DefaultMaxAge;
        }

        // validation successful - return client
        return new DynamicClientRegistrationValidatedRequest(client, request);
    }
}