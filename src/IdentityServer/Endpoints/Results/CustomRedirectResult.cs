// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;

namespace Duende.IdentityServer.Endpoints.Results;

/// <summary>
/// Result for a custom redirect
/// </summary>
public class CustomRedirectResult : InteractivePageResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomRedirectResult"/> class.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="url">The URL.</param>
    /// <param name="options"></param>
    /// <exception cref="System.ArgumentNullException">
    /// request
    /// or
    /// url
    /// </exception>
    public CustomRedirectResult(ValidatedAuthorizeRequest request, string url, IdentityServerOptions options)
        : base(request, url, options.UserInteraction.CustomRedirectReturnUrlParameter)
    {
    }

    internal CustomRedirectResult(
        ValidatedAuthorizeRequest request,
        string url,
        IdentityServerOptions options,
        IServerUrls urls,
        IAuthorizationParametersMessageStore authorizationParametersMessageStore = null) 
        : base(request, url, options.UserInteraction.CustomRedirectReturnUrlParameter, urls, authorizationParametersMessageStore)
    {
    }
}