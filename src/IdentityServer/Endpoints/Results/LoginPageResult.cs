// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;

namespace Duende.IdentityServer.Endpoints.Results;

/// <summary>
/// Result for login page
/// </summary>
public class LoginPageResult : InteractivePageResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginPageResult"/> class.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="options"></param>
    /// <exception cref="System.ArgumentNullException">request</exception>
    public LoginPageResult(ValidatedAuthorizeRequest request, IdentityServerOptions options) 
        : base(request, options.UserInteraction.LoginUrl, options.UserInteraction.LoginReturnUrlParameter)
    {
    }

    internal LoginPageResult(
        ValidatedAuthorizeRequest request,
        IdentityServerOptions options,
        IServerUrls urls,
        IAuthorizationParametersMessageStore authorizationParametersMessageStore = null) 
        : base(request, options.UserInteraction.LoginUrl, options.UserInteraction.LoginReturnUrlParameter, urls, authorizationParametersMessageStore)
    {
    }
}