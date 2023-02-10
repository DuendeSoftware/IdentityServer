using System.Security.Claims;
using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;

namespace Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

public interface IDynamicClientRegistrationValidator
{
    Task<DynamicClientRegistrationValidationResult> ValidateAsync(ClaimsPrincipal caller, DynamicClientRegistrationRequest request);
}