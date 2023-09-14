// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;
using Microsoft.AspNetCore.Http;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;
using static Duende.IdentityServer.IdentityServerConstants;
using IdentityModel;

namespace Duende.IdentityServer.Endpoints.Results;

/// <summary>
/// Result for an interactive page
/// </summary>
/// <seealso cref="IEndpointResult" />
public abstract class AuthorizeInteractionPageResult : EndpointResult<AuthorizeInteractionPageResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizeInteractionPageResult"/> class.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="redirectUrl"></param>
    /// <param name="returnUrlParameterName"></param>
    /// <exception cref="System.ArgumentNullException">request</exception>
    public AuthorizeInteractionPageResult(ValidatedAuthorizeRequest request, string redirectUrl, string returnUrlParameterName)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        RedirectUrl = redirectUrl ?? throw new ArgumentNullException(nameof(redirectUrl));
        ReturnUrlParameterName = returnUrlParameterName ?? throw new ArgumentNullException(nameof(returnUrlParameterName));
    }

    /// <summary>
    /// The validated authorize request
    /// </summary>
    public ValidatedAuthorizeRequest Request { get; }

    /// <summary>
    /// The redirect URI
    /// </summary>
    public string RedirectUrl { get; }

    /// <summary>
    /// The return URL param name
    /// </summary>
    public string ReturnUrlParameterName { get; }
}

class AuthorizeInteractionPageResultGenerator : IEndpointResultGenerator<AuthorizeInteractionPageResult>
{
    private readonly IServerUrls _urls;
    private readonly IAuthorizationParametersMessageStore _authorizationParametersMessageStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizeInteractionPageResult"/> class.
    /// </summary>
    public AuthorizeInteractionPageResultGenerator(IServerUrls urls, IAuthorizationParametersMessageStore authorizationParametersMessageStore = null)
    {
        _urls = urls;
        _authorizationParametersMessageStore = authorizationParametersMessageStore;
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(AuthorizeInteractionPageResult result, HttpContext context)
    {
        var returnUrl = _urls.BasePath.EnsureTrailingSlash() + ProtocolRoutePaths.AuthorizeCallback;

        //TODO - Verify that we can use both PAR and the parameter message store
        if (_authorizationParametersMessageStore != null)
        {
            var msg = new Message<IDictionary<string, string[]>>(result.Request.ToOptimizedFullDictionary());
            var id = await _authorizationParametersMessageStore.WriteAsync(msg);
            returnUrl = returnUrl.AddQueryString(Constants.AuthorizationParamsStore.MessageStoreIdParameterName, id);
        }
        else
        {
            // TODO - Verify that this is okay when we are using JAR Request URIs, but not PAR
            // TODO - Consider changing the Request model to make it more obvious that this is for PAR
            if (result.Request.RequestUri != null)
            {
                returnUrl = returnUrl
                    .AddQueryString(OidcConstants.AuthorizeRequest.RequestUri, result.Request.RequestUri)
                    .AddQueryString(OidcConstants.AuthorizeRequest.ClientId, result.Request.ClientId);
            } 
            else
            {
                returnUrl = returnUrl.AddQueryString(result.Request.ToOptimizedQueryString());
            }
        }

        var url = result.RedirectUrl;
        if (!url.IsLocalUrl())
        {
            // this converts the relative redirect path to an absolute one if we're 
            // redirecting to a different server
            returnUrl = _urls.Origin + returnUrl;
        }

        url = url.AddQueryString(result.ReturnUrlParameterName, returnUrl);
        context.Response.Redirect(_urls.GetAbsoluteUrl(url));
    }
}