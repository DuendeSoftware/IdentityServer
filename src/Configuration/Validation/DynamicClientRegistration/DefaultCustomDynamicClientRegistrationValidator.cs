using System.Security.Claims;

namespace Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

public class DefaultCustomDynamicClientRegistrationValidator : ICustomDynamicClientRegistrationValidator
{
    public async Task<DynamicClientRegistrationValidationResult> ValidateAsync(ClaimsPrincipal caller, DynamicClientRegistrationValidatedRequest request)
    {
        return await Task.FromResult(request);
    }
}