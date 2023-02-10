using System.Security.Claims;

namespace Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

public interface ICustomDynamicClientRegistrationValidator
{
    Task<DynamicClientRegistrationValidationResult> ValidateAsync(ClaimsPrincipal caller, DynamicClientRegistrationValidatedRequest request);
}