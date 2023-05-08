// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

namespace Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

/// <summary>
/// Validates a dynamic client registration request.
/// </summary>
public interface IDynamicClientRegistrationValidator
{
    /// <summary>
    /// Validates a dynamic client registration request. 
    /// </summary>
    /// <param name="context">Contextual information about the DCR request.</param>
    /// <returns>A task that returns a <see
    /// cref="DynamicClientRegistrationValidationResult"/>, which is either a
    /// model of the validated request or a validation error.</returns>
    Task<DynamicClientRegistrationValidationResult> ValidateAsync(DynamicClientRegistrationValidationContext context);
}
