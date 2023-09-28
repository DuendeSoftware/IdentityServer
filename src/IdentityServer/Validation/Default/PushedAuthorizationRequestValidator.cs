// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Duende.IdentityServer.Extensions;
using IdentityModel;

namespace Duende.IdentityServer.Validation;

/// <summary>
/// Validates API secrets using the registered secret validators and parsers
/// </summary>
public class PushedAuthorizationRequestValidator : IPushedAuthorizationRequestValidator
{
    public Task<PushedAuthorizationValidationResult> ValidateAsync(PushedAuthorizationRequestValidationContext context)
    {
        // Reject request_uri parameter
        if (context.RequestParameters.Get(OidcConstants.AuthorizeRequest.RequestUri).IsPresent())
        {
            return Task.FromResult(new PushedAuthorizationValidationResult("invalid_request", "Pushed authorization cannot use request_uri"));
        }
        else
        {
            return Task.FromResult(new PushedAuthorizationValidationResult(new ValidatedPushedAuthorizationRequest
            {
                Raw = context.RequestParameters,
                Client = context.Client
            }));
        }
    }
}