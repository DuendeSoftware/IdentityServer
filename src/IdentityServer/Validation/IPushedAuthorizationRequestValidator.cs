// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation;


public interface IPushedAuthorizationRequestValidator
{
    Task<PushedAuthorizationValidationResult> ValidateAsync(PushedAuthorizationRequestValidationContext context);
}