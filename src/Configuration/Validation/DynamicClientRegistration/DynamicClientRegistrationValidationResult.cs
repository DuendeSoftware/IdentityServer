using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

public abstract record DynamicClientRegistrationValidationResult { }

public record DynamicClientRegistrationValidationError(string Error, string ErrorDescription) : DynamicClientRegistrationValidationResult;

public record DynamicClientRegistrationValidatedRequest(Client Client, DynamicClientRegistrationRequest Original) : DynamicClientRegistrationValidationResult;

