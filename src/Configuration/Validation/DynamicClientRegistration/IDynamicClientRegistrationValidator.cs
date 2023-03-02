// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Security.Claims;
using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;

namespace Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

public interface IDynamicClientRegistrationValidator
{
    Task<DynamicClientRegistrationValidationResult> ValidateAsync(ClaimsPrincipal caller, DynamicClientRegistrationRequest request);
}