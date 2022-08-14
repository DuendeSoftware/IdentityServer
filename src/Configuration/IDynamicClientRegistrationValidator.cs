using System.Security.Claims;

namespace Duende.IdentityServer.Configuration;

public interface IDynamicClientRegistrationValidator
{
    Task<DynamicClientRegistrationValidationResult> ValidateAsync(ClaimsPrincipal caller, DynamicClientRegistrationDocument document);
}