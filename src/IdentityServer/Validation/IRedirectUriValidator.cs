// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

using Duende.IdentityServer.Models;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation;

/// <summary>
/// Models the logic when validating redirect and post logout redirect URIs.
/// </summary>
public interface IRedirectUriValidator
{
    /// <summary>
    /// Determines whether a redirect URI is valid for a client.
    /// </summary>
    /// <param name="requestedUri">The requested URI.</param>
    /// <param name="client">The client.</param>
    /// <returns><c>true</c> is the URI is valid; <c>false</c> otherwise.</returns>
    Task<bool> IsRedirectUriValidAsync(string requestedUri, Client client);

    /// <summary>
    /// Determines whether a redirect URI is valid for a client.
    /// </summary>
    Task<bool> IsRedirectUriValidAsync(RedirectUriValidationContext context) => IsRedirectUriValidAsync(context.RequestedUri, context.Client);

    /// <summary>
    /// Determines whether a post logout URI is valid for a client.
    /// </summary>
    /// <param name="requestedUri">The requested URI.</param>
    /// <param name="client">The client.</param>
    /// <returns><c>true</c> is the URI is valid; <c>false</c> otherwise.</returns>
    Task<bool> IsPostLogoutRedirectUriValidAsync(string requestedUri, Client client);
}

/// <summary>
/// Models the context for validating a client's redirect URI
/// </summary>
public class RedirectUriValidationContext
{
    /// <summary>
    /// Default ctor
    /// </summary>
    public RedirectUriValidationContext()
    {
    }

    /// <summary>
    /// ctor
    /// </summary>
    public RedirectUriValidationContext(string redirectUri, ValidatedAuthorizeRequest request)
    {
        RequestedUri = redirectUri;
        Client = request.Client;
        RequestParameters = request.Raw;
        RequestObjectValues = request.RequestObjectValues;
    }

    /// <summary>
    /// The URI to validate for the client
    /// </summary>
    public string RequestedUri { get; set; } = default!;

    /// <summary>
    /// The client
    /// </summary>
    public Client Client { get; set; } = default!;

    /// <summary>
    /// Gets or sets the request parameters
    /// </summary>
    public NameValueCollection RequestParameters { get; set; } = default!;

    /// <summary>
    /// Validated request object values
    /// </summary>
    public IEnumerable<Claim>? RequestObjectValues { get; set; }
}
