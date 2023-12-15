// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Duende.IdentityServer.Extensions;
using IdentityModel;
using Microsoft.AspNetCore.Http;
using System;
using System.Text.Encodings.Web;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;

namespace Duende.IdentityServer.Endpoints.Results;

/// <summary>
/// Models the result from the authorize endpoint
/// </summary>
public class AuthorizeResult : EndpointResult<AuthorizeResult>
{
    /// <summary>
    /// The authorize response
    /// </summary>
    public AuthorizeResponse Response { get; }

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="response"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public AuthorizeResult(AuthorizeResponse response)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
    }
}

/// <summary>
/// Writes http responses for <see cref="AuthorizeResult"/>s.
/// </summary>
public class AuthorizeHttpWriter : IHttpResponseWriter<AuthorizeResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizeHttpWriter"/> class.
    /// </summary>
    public AuthorizeHttpWriter(
        IdentityServerOptions options,
        IUserSession userSession,
        IPushedAuthorizationService pushedAuthorizationService,
        IMessageStore<ErrorMessage> errorMessageStore,
        IServerUrls urls,
        IClock clock)
    {
        _options = options;
        _userSession = userSession;
        _errorMessageStore = errorMessageStore;
        _urls = urls;
        _clock = clock;
        _pushedAuthorizationService = pushedAuthorizationService;
    }

    private readonly IdentityServerOptions _options;
    private readonly IUserSession _userSession;
    private readonly IPushedAuthorizationService _pushedAuthorizationService;
    private readonly IMessageStore<ErrorMessage> _errorMessageStore;
    private readonly IServerUrls _urls;
    private readonly IClock _clock;

    /// <inheritdoc />
    public async Task WriteHttpResponse(AuthorizeResult result, HttpContext context)
    {
        await ConsumePushedAuthorizationRequest(result);

        if (result.Response.IsError)
        {
            await ProcessErrorAsync(result.Response, context);
        }
        else
        {
            await ProcessResponseAsync(result.Response, context);
        }
    }

    private async Task ConsumePushedAuthorizationRequest(AuthorizeResult result)
    {
        var referenceValue = result.Response?.Request?.PushedAuthorizationReferenceValue;
        if(referenceValue.IsPresent())
        {
            await _pushedAuthorizationService.ConsumeAsync(referenceValue);
        }
    }

    private async Task ProcessErrorAsync(AuthorizeResponse response, HttpContext context)
    {
        // these are the conditions where we can send a response 
        // back directly to the client, otherwise we're only showing the error UI
        var isSafeError =
            response.Error == OidcConstants.AuthorizeErrors.AccessDenied ||
            response.Error == OidcConstants.AuthorizeErrors.AccountSelectionRequired ||
            response.Error == OidcConstants.AuthorizeErrors.LoginRequired ||
            response.Error == OidcConstants.AuthorizeErrors.ConsentRequired ||
            response.Error == OidcConstants.AuthorizeErrors.InteractionRequired ||
            response.Error == OidcConstants.AuthorizeErrors.TemporarilyUnavailable ||
            response.Error == OidcConstants.AuthorizeErrors.UnmetAuthenticationRequirements;
        if (isSafeError)
        {
            // this scenario we can return back to the client
            await ProcessResponseAsync(response, context);
        }
        else
        {
            // we now know we must show error page
            await RedirectToErrorPageAsync(response, context);
        }
    }

    private async Task ProcessResponseAsync(AuthorizeResponse response, HttpContext context)
    {
        if (!response.IsError)
        {
            // success response -- track client authorization for sign-out
            //_logger.LogDebug("Adding client {0} to client list cookie for subject {1}", request.ClientId, request.Subject.GetSubjectId());
            await _userSession.AddClientIdAsync(response.Request.ClientId);
        }

        await RenderAuthorizeResponseAsync(response, context);
    }

    private async Task RenderAuthorizeResponseAsync(AuthorizeResponse response, HttpContext context)
    {
        if (response.Request.ResponseMode == OidcConstants.ResponseModes.Query ||
            response.Request.ResponseMode == OidcConstants.ResponseModes.Fragment)
        {
            context.Response.SetNoCache();
            context.Response.Redirect(BuildRedirectUri(response));
        }
        else if (response.Request.ResponseMode == OidcConstants.ResponseModes.FormPost)
        {
            context.Response.SetNoCache();
            AddSecurityHeaders(context);
            await context.Response.WriteHtmlAsync(GetFormPostHtml(response));
        }
        else
        {
            //_logger.LogError("Unsupported response mode.");
            throw new InvalidOperationException("Unsupported response mode");
        }
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        context.Response.AddScriptCspHeaders(_options.Csp, IdentityServerConstants.ContentSecurityPolicyHashes.AuthorizeScript);

        var referrer_policy = "no-referrer";
        if (!context.Response.Headers.ContainsKey("Referrer-Policy"))
        {
            context.Response.Headers.Append("Referrer-Policy", referrer_policy);
        }
    }

    private string BuildRedirectUri(AuthorizeResponse response)
    {
        var uri = response.RedirectUri;
        var query = response.ToNameValueCollection(_options).ToQueryString();

        if (response.Request.ResponseMode == OidcConstants.ResponseModes.Query)
        {
            uri = uri.AddQueryString(query);
        }
        else
        {
            uri = uri.AddHashFragment(query);
        }

        if (response.IsError && !uri.Contains("#"))
        {
            // https://tools.ietf.org/html/draft-bradley-oauth-open-redirector-00
            uri += "#_=_";
        }

        return uri;
    }

    private const string DefaultFormPostHeadTags = "<head><meta http-equiv='X-UA-Compatible' content='IE=edge' /><base target='_self'/></head>";
    private const string DefaultFormPostBodyTags = "<body><form method='post' action='{uri}'>{body}<noscript><button>Click to continue</button></noscript></form><script>window.addEventListener('load', function(){document.forms[0].submit();});</script></body>";

    /// <summary>
    /// Gets the header tags that will be included in the response when
    /// response_mode is form_post.
    /// </summary>
    protected virtual string FormPostHeader => DefaultFormPostHeadTags;
    
    /// <summary>
    /// Gets the body tags that will be included in the response when
    /// response_mode is form_post. The string "{body}" (including the curly
    /// braces) within this string will be replaced with the response
    /// parameters, serialized as form data.
    /// </summary>
    protected virtual string FormPostBody => DefaultFormPostBodyTags;

    /// <summary>
    /// Gets the html that will set as the response when response_mode is
    /// form_post. 
    /// </summary>
    /// <param name="response"></param>
    protected virtual string GetFormPostHtml(AuthorizeResponse response)
    {
        var html = $"<html>{FormPostHeader}{FormPostBody}</html>";

        var url = response.Request.RedirectUri;
        url = HtmlEncoder.Default.Encode(url);
        html = html.Replace("{uri}", url);
        html = html.Replace("{body}", response.ToNameValueCollection(_options).ToFormPost());

        return html;
    }

    private async Task RedirectToErrorPageAsync(AuthorizeResponse response, HttpContext context)
    {
        var errorModel = new ErrorMessage
        {
            RequestId = context.TraceIdentifier,
            Error = response.Error,
            ErrorDescription = response.ErrorDescription,
            UiLocales = response.Request?.UiLocales,
            DisplayMode = response.Request?.DisplayMode,
            ClientId = response.Request?.ClientId
        };

        if (response.RedirectUri != null && response.Request?.ResponseMode != null)
        {
            // if we have a valid redirect uri, then include it to the error page
            errorModel.RedirectUri = BuildRedirectUri(response);
            errorModel.ResponseMode = response.Request.ResponseMode;
        }

        var message = new Message<ErrorMessage>(errorModel, _clock.UtcNow.UtcDateTime);
        var id = await _errorMessageStore.WriteAsync(message);

        var errorUrl = _options.UserInteraction.ErrorUrl;

        var url = errorUrl.AddQueryString(_options.UserInteraction.ErrorIdParameter, id);
        context.Response.Redirect(_urls.GetAbsoluteUrl(url));
    }
}