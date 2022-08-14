using System.Security.Claims;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Configuration;

public class DefaultCustomDynamicClientRegistrationValidator : ICustomDynamicClientRegistrationValidator
{
    public Task<DynamicClientRegistrationValidationResult> ValidateAsync(ClaimsPrincipal caller, DynamicClientRegistrationRequest request, Client client)
    {
        return Task.FromResult(new DynamicClientRegistrationValidationResult(client));
    }
}