using System.Security.Claims;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Configuration;

public interface ICustomDynamicClientRegistrationValidator
{
    Task<DynamicClientRegistrationValidationResult> ValidateAsync(ClaimsPrincipal caller, DynamicClientRegistrationDocument document, Client client);
}