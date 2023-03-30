// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Security.Claims;
using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;

namespace Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

/// <summary>
/// Validates a dynamic client registration request.
/// </summary>
public interface IDynamicClientRegistrationValidator
{
    /// <summary>
    /// Validates a dynamic client registration request. 
    /// </summary>
    /// <param name="request">The dynamic client registration request to be
    /// validated.</param>
    /// <param name="caller">The claims principal of the caller making the
    /// request.</param>
    /// <returns>A task that returns a <see
    /// cref="DynamicClientRegistrationValidationResult"/>, which is either a
    /// model of the validated request or a validation error.</returns>
    Task<DynamicClientRegistrationValidationResult> ValidateAsync(DynamicClientRegistrationRequest request, ClaimsPrincipal caller);
}
