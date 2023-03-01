using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

namespace Duende.IdentityServer.Configuration
{
    public interface IDynamicClientRegistrationRequestProcessor
    {
        Task<DynamicClientRegistrationResponse> ProcessAsync(DynamicClientRegistrationValidatedRequest validatedRequest);
    }
}