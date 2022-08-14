using System.Security.Claims;

namespace Duende.IdentityServer.Configuration;

public interface IDynamicClientRegistrationValidator
{
    Task<DynamicClientRegistrationValidationResult> ValidateAsync(ClaimsPrincipal caller, DynamicClientRegistrationRequest request);
}