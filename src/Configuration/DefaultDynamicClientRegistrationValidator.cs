using System.Security.Claims;
using Duende.IdentityServer.Models;
using IdentityModel;

namespace Duende.IdentityServer.Configuration;

public class DefaultDynamicClientRegistrationValidator : IDynamicClientRegistrationValidator
{
    public Task<DynamicClientRegistrationValidationResult> ValidateAsync(ClaimsPrincipal caller, DynamicClientRegistrationDocument document)
    {
        var client = new Client
        {
            ClientId = CryptoRandom.CreateUniqueId()
        };
        
        //////////////////////////////
        // validate grant types
        //////////////////////////////
        if (document.GrantTypes.Contains(OidcConstants.GrantTypes.ClientCredentials))
        {
            client.AllowedGrantTypes.Add(GrantType.ClientCredentials);
        }
        if (document.GrantTypes.Contains(OidcConstants.GrantTypes.AuthorizationCode))
        {
            client.AllowedGrantTypes.Add(GrantType.AuthorizationCode);
        }

        // we only support the two above grant types
        if (client.AllowedGrantTypes.Count == 0)
        {
            return Task.FromResult(new DynamicClientRegistrationValidationResult(DynamicClientRegistrationErrors.InvalidClientMetadata, "unsupported grant type"));
        }

        if (document.GrantTypes.Contains(OidcConstants.GrantTypes.RefreshToken))
        {
            if (client.AllowedGrantTypes.Count == 1 &&
                client.AllowedGrantTypes.FirstOrDefault(t => t.Equals(GrantType.ClientCredentials)) != null)
            {
                return Task.FromResult(new DynamicClientRegistrationValidationResult(DynamicClientRegistrationErrors.InvalidClientMetadata, "client credentials does not support refresh tokens"));
            }

            client.AllowOfflineAccess = true;
        }
        
        //////////////////////////////
        // validate redirect URIs
        //////////////////////////////
        if (client.AllowedGrantTypes.Contains(GrantType.AuthorizationCode))
        {
            if (document.RedirectUris.Any())
            {
                foreach (var requestRedirectUri in document.RedirectUris)
                {
                    if (requestRedirectUri.IsAbsoluteUri)
                    {
                        client.RedirectUris.Add(requestRedirectUri.AbsoluteUri);    
                    }
                    else
                    {
                        return Task.FromResult(new DynamicClientRegistrationValidationResult(DynamicClientRegistrationErrors.InvalidRedirectUri ,"malformed redirect URI"));
                    }
                }
            }
            else
            {
                return Task.FromResult(new DynamicClientRegistrationValidationResult(DynamicClientRegistrationErrors.InvalidRedirectUri, "redirect URI required for authorization_code grant type"));
            }
        }

        if (client.AllowedGrantTypes.Count == 1 &&
            client.AllowedGrantTypes.FirstOrDefault(t => t.Equals(GrantType.ClientCredentials)) != null)
        {
            if (document.RedirectUris.Any())
            {
                return Task.FromResult(new DynamicClientRegistrationValidationResult(DynamicClientRegistrationErrors.InvalidRedirectUri, "redirect URI not compatible with client_credentials grant type"));
            }
        }
        
        //////////////////////////////
        // validate scopes
        //////////////////////////////
        if (string.IsNullOrEmpty(document.Scope))
        {
            // todo: what to do when scopes are missing? error - leave up to custom validator?
        }
        else
        {
            var scopes = document.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // todo: ideally scope names get checked against configuration store?
            
            foreach (var scope in scopes)
            {
                client.AllowedScopes.Add(scope);
            }
        }
        
        //////////////////////////////
        // secret handling
        //////////////////////////////
        
        // todo: if jwks is present - convert JWKs into secrets
        // todo: add jwks_uri suppport
        
        //////////////////////////////
        // misc
        //////////////////////////////
        if (!string.IsNullOrWhiteSpace(document.ClientName))
        {
            client.ClientName = document.ClientName;
        }
        
        if (document.ClientUri != null)
        {
            client.ClientUri = document.ClientUri.AbsoluteUri;
        }

        if (document.DefaultMaxAge.HasValue)
        {
            client.UserSsoLifetime = document.DefaultMaxAge;
        }
        
        // validation successful - return client
        return Task.FromResult(new DynamicClientRegistrationValidationResult(client)); 
    }
}