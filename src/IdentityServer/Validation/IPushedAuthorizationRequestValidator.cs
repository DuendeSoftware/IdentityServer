// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation;

/// <summary>
/// Validation service for pushed authorization requests. Note that the pushed authorization parameters are additionally
/// validated using the <see cref="AuthorizeRequestValidator"/>. This service performs validation that is specific to
/// pushed authorization requests. 
/// </summary>
public interface IPushedAuthorizationRequestValidator
{
    /// <summary>
    /// Validates the pushed authorization request.
    /// </summary>
    /// <param name="context">The validation context</param>
    /// <returns>A pushed authorization result that either wraps the validated request values or indicates the
    /// error code and description.</returns>
    Task<PushedAuthorizationValidationResult> ValidateAsync(PushedAuthorizationRequestValidationContext context);
}