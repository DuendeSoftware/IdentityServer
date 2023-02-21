using System.Security.Claims;
using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using IdentityModel;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

public class DefaultDynamicClientRegistrationValidator : IDynamicClientRegistrationValidator
{
    private readonly IResourceStore _resources;
    private readonly ILogger<DefaultDynamicClientRegistrationValidator> _logger;

    public DefaultDynamicClientRegistrationValidator(IResourceStore resources, ILogger<DefaultDynamicClientRegistrationValidator> logger)
    {
        _resources = resources;
        _logger = logger;
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

            var validApiScopes = await _resources.FindApiScopesByNameAsync(scopes);
            var validIdentityResources = await _resources.FindIdentityResourcesByScopeNameAsync(scopes);

            if(validApiScopes.Count() + validIdentityResources.Count() != scopes.Length)
            {
                var validScopeNames = validApiScopes.Select(s => s.Name).Concat(validIdentityResources.Select(s => s.Name));
                var invalidScopeNames = string.Join(" ", scopes.Except(validScopeNames));

                return new DynamicClientRegistrationValidationError(
                    DynamicClientRegistrationErrors.InvalidClientMetadata,
                    $"unsupported scope: {invalidScopeNames}"
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

        // todo: if jwks is present - convert JWKs into secrets
        // todo: add jwks_uri support

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