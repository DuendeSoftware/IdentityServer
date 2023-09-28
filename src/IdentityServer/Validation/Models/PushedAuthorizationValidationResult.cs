// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

namespace Duende.IdentityServer.Validation;

public class PushedAuthorizationValidationResult : ValidationResult
{
    public PushedAuthorizationValidationResult(ValidatedPushedAuthorizationRequest validatedRequest)
    {
        IsError = false;
        ValidatedRequest = validatedRequest;
    }

    public PushedAuthorizationValidationResult(string error, string errorDescription)
    {
        IsError = true;
        Error = error;
        ErrorDescription = errorDescription;
    }

    public ValidatedPushedAuthorizationRequest? ValidatedRequest { get; }
}
