// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Extensions;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation;

/// <summary>
/// Implementation of URI validator that allows any uri to be pushed and then used.
/// </summary>
/// <seealso cref="StrictRedirectUriValidator" />
public class ParRedirectUriValidator : StrictRedirectUriValidator
{
    /// <summary>
    /// Determines whether a redirect URI is valid for a client.
    /// </summary>
    /// <param name="context"></param>
    /// <returns>
    ///   <c>true</c> is the URI is valid; <c>false</c> otherwise.
    /// </returns>
    public override Task<bool> IsRedirectUriValidAsync(RedirectUriValidationContext context)
    {
        // Any pushed redirect uri is allowed on the PAR endpoint
        if(context.AuthorizeRequestType == AuthorizeRequestType.PushedAuthorizationRequest)
        {
            if (context.RequestParameters.Get(IdentityModel.OidcConstants.AuthorizeRequest.RedirectUri).IsPresent())
            {
                return Task.FromResult(true);
            }
        }
        // On the authorize endpoint, if a redirect uri was pushed, that is also valid
        if(context.AuthorizeRequestType == AuthorizeRequestType.AuthorizeRequestWithPushedParameters)
        {
            if(context.RequestedUri == context.RequestParameters.Get(IdentityModel.OidcConstants.AuthorizeRequest.RedirectUri))
            {
                return Task.FromResult(true);
            }
        }
        // Otherwise, just use the default strict validation
        return base.IsRedirectUriValidAsync(context.RequestedUri, context.Client);
    }
}