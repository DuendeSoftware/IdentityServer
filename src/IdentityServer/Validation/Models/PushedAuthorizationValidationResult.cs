// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

namespace Duende.IdentityServer.Validation;

/// <summary>
/// Represents the results of validating a pushed authorization request.
/// </summary>
public class PushedAuthorizationValidationResult : ValidationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PushedAuthorizationValidationResult"/> class.
    /// </summary>
    /// <param name="validatedRequest">The validated request.</param>
    public PushedAuthorizationValidationResult(ValidatedPushedAuthorizationRequest validatedRequest)
    {
        IsError = false;
        ValidatedRequest = validatedRequest;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PushedAuthorizationValidationResult"/> class.
    /// </summary>
    /// <param name="error">The error code, as specified by RFC 9126, etc</param>
    /// <param name="errorDescription">The error description: "human-readable ASCII text providing
    /// additional information, used to assist the client developer in
    /// understanding the error that occurred."</param>
    public PushedAuthorizationValidationResult(string error, string errorDescription)
    {
        IsError = true;
        Error = error;
        ErrorDescription = errorDescription;
    }

    /// <summary>
    /// The validated pushed authorization request, or null, if a validation error occured. 
    /// </summary>
    public ValidatedPushedAuthorizationRequest? ValidatedRequest { get; }
}
