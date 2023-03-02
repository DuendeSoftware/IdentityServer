// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

namespace Duende.IdentityServer.Configuration
{
    public interface IDynamicClientRegistrationRequestProcessor
    {
        Task<DynamicClientRegistrationResponse> ProcessAsync(DynamicClientRegistrationValidatedRequest validatedRequest);
    }
}