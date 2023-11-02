// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation;

/// <summary>
/// Default implementation of redirect URI validator. Validates the URIs against
/// the client's configured URIs.
/// </summary>
public class StrictRedirectUriValidator : IRedirectUriValidator
{
    /// <summary>
    /// Checks if a given URI string is in a collection of strings (using ordinal ignore case comparison)
    /// </summary>
    /// <param name="uris">The uris.</param>
    /// <param name="requestedUri">The requested URI.</param>
    /// <returns></returns>
    protected bool StringCollectionContainsString(IEnumerable<string> uris, string requestedUri)
    {
        if (IEnumerableExtensions.IsNullOrEmpty(uris)) return false;

        return uris.Contains(requestedUri, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether a redirect URI is valid for a client.
    /// </summary>
    /// <param name="requestedUri">The requested URI.</param>
    /// <param name="client">The client.</param>
    /// <returns>
    ///   <c>true</c> is the URI is valid; <c>false</c> otherwise.
    /// </returns>
    public virtual Task<bool> IsRedirectUriValidAsync(string requestedUri, Client client)
    {
        return Task.FromResult(StringCollectionContainsString(client.RedirectUris, requestedUri));
    }

    /// <summary>
    /// Determines whether a post logout URI is valid for a client.
    /// </summary>
    /// <param name="requestedUri">The requested URI.</param>
    /// <param name="client">The client.</param>
    /// <returns>
    ///   <c>true</c> is the URI is valid; <c>false</c> otherwise.
    /// </returns>
    public virtual Task<bool> IsPostLogoutRedirectUriValidAsync(string requestedUri, Client client)
    {
        return Task.FromResult(StringCollectionContainsString(client.PostLogoutRedirectUris, requestedUri));
    }

    /// <summary>
    /// Determines whether a redirect uri is valid for a context.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>
    ///   <c>true</c> is the URI is valid; <c>false</c> otherwise.
    /// </returns>
    public virtual Task<bool> IsRedirectUriValidAsync(RedirectUriValidationContext context)
        // This method is identical to the default implementation on
        // IRedirectUriValidator, but is virtual so that derived classes can
        // override it. Leaving the default implementation in the interface
        // avoids a breaking change for customizations, even though our default
        // implementations no longer need it.
        => IsRedirectUriValidAsync(context.RequestedUri, context.Client);
}