using System.Security.Claims;
using Duende.IdentityServer.Models;
using IdentityModel;

namespace Duende.IdentityServer.Configuration;

public class DefaultDynamicClientRegistrationValidator : IDynamicClientRegistrationValidator
{
    public Task<DynamicClientRegistrationValidationResult> ValidateAsync(ClaimsPrincipal caller, DynamicClientRegistrationDocument request)
    {
        var client = new Client
        {
            ClientId = CryptoRandom.CreateUniqueId()
        };
        
        //////////////////////////////
        // validate grant types
        //////////////////////////////
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
            return Task.FromResult(new DynamicClientRegistrationValidationResult(DynamicClientRegistrationError.InvalidClientMetadata, "unsupported grant type"));
        }

        if (request.GrantTypes.Contains(OidcConstants.GrantTypes.RefreshToken))
        {
            if (client.AllowedGrantTypes.Count == 1 &&
                client.AllowedGrantTypes.FirstOrDefault(t => t.Equals(GrantType.ClientCredentials)) != null)
            {
                return Task.FromResult(new DynamicClientRegistrationValidationResult(DynamicClientRegistrationError.InvalidClientMetadata, "client credentials does not support refresh tokens"));
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
                        return Task.FromResult(new DynamicClientRegistrationValidationResult(DynamicClientRegistrationError.InvalidRedirectUri ,"malformed redirect URI"));
                    }
                }
            }
            else
            {
                return Task.FromResult(new DynamicClientRegistrationValidationResult(DynamicClientRegistrationError.InvalidRedirectUri, "redirect URI required for authorization_code grant type"));
            }
        }

        if (client.AllowedGrantTypes.Count == 1 &&
            client.AllowedGrantTypes.FirstOrDefault(t => t.Equals(GrantType.ClientCredentials)) != null)
        {
            if (request.RedirectUris.Any())
            {
                return Task.FromResult(new DynamicClientRegistrationValidationResult(DynamicClientRegistrationError.InvalidRedirectUri, "redirect URI not compatible with client_credentials grant type"));
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
        if (!string.IsNullOrWhiteSpace(request.ClientName))
        {
            client.ClientName = request.ClientName;
        }
        
        if (request.ClientUri != null)
        {
            client.ClientUri = request.ClientUri.AbsoluteUri;
        }
        
        // validation successful - return client
        return Task.FromResult(new DynamicClientRegistrationValidationResult(client)); 
    }
}