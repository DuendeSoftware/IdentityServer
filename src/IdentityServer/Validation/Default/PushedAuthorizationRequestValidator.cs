﻿// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

using System.Threading.Tasks;
using Duende.IdentityServer.Extensions;
using IdentityModel;

namespace Duende.IdentityServer.Validation;

/// <summary>
/// Default validator for pushed authorization requests. This validator performs
/// checks that are specific to pushed authorization and also invokes the <see
/// cref="IAuthorizeRequestValidator"/> to validate the pushed parameters as if
/// they had been sent to the authorize endpoint directly. 
/// </summary>
internal class PushedAuthorizationRequestValidator : IPushedAuthorizationRequestValidator
{

    private readonly IAuthorizeRequestValidator _authorizeRequestValidator;

    /// <summary>
    /// Initializes a new instance of the <see
    /// cref="PushedAuthorizationRequestValidator"/> class. 
    /// </summary>
    /// <param name="authorizeRequestValidator">The authorize request validator,
    /// used to validate the pushed authorization parameters as if they were
    /// used directly at the authorize endpoint.</param>
    public PushedAuthorizationRequestValidator(IAuthorizeRequestValidator authorizeRequestValidator)
    {
        _authorizeRequestValidator = authorizeRequestValidator;
    }

    /// <inheritdoc />
    public async Task<PushedAuthorizationValidationResult> ValidateAsync(PushedAuthorizationRequestValidationContext context)
    {
        IdentityServerLicenseValidator.Instance.ValidatePar();
        var validatedRequest = await ValidateRequestUriAsync(context);
        if(validatedRequest.IsError)
        {
            return validatedRequest;
        }

        var authorizeRequestValidation = await _authorizeRequestValidator.ValidateAsync(context.RequestParameters, 
            authorizeRequestType: AuthorizeRequestType.PushedAuthorization);
        if(authorizeRequestValidation.IsError)
        {
            return new PushedAuthorizationValidationResult(
                authorizeRequestValidation.Error,
                authorizeRequestValidation.ErrorDescription,
                authorizeRequestValidation.ValidatedRequest);
        }

        return validatedRequest;
    }

    /// <summary>
    /// Validates a PAR request to ensure that it does not contain a request
    /// URI, which is explicitly disallowed by RFC 9126.
    /// </summary>
    /// <param name="context">The pushed authorization validation
    /// context.</param>
    /// <returns>A task containing the <see
    /// cref="PushedAuthorizationValidationResult"/>.</returns>
    private Task<PushedAuthorizationValidationResult> ValidateRequestUriAsync(PushedAuthorizationRequestValidationContext context)
    {
        // Reject request_uri parameter
        if (context.RequestParameters.Get(OidcConstants.AuthorizeRequest.RequestUri).IsPresent())
        {
            return Task.FromResult(new PushedAuthorizationValidationResult("invalid_request", "Pushed authorization cannot use request_uri"));
        }
        else
        {
            return Task.FromResult(new PushedAuthorizationValidationResult(
                new ValidatedPushedAuthorizationRequest
                {
                    Raw = context.RequestParameters,
                    Client = context.Client
                }));
        }
    }
}