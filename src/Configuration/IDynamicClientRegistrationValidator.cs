using System.Security.Claims;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Configuration;

public interface IDynamicClientRegistrationValidator
{
    Task<DynamicClientRegistrationValidationResult> ValidateAsync(ClaimsPrincipal caller, DynamicClientRegistrationDocument request);
}