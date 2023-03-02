// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;
using Microsoft.AspNetCore.Http;

namespace Duende.IdentityServer.Configuration.ResponseGeneration;

public interface IDynamicClientRegistrationResponseGenerator
{
    Task WriteBadRequestError(HttpContext context);
    Task WriteResponse<T>(HttpContext context, int statusCode, T response)
        where T : IDynamicClientRegistrationResponse;
    Task WriteSuccessResponse(HttpContext context, DynamicClientRegistrationResponse response);
    Task WriteValidationError(HttpContext context, DynamicClientRegistrationValidationError error);
}