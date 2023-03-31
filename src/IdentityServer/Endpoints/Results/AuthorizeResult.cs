// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Duende.IdentityServer.Extensions;
using IdentityModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.AspNetCore.Authentication;
using System.Text.Encodings.Web;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;

namespace Duende.IdentityServer.Endpoints.Results;

internal class AuthorizeResult : IEndpointResult
{
    public AuthorizeResponse Response { get; }

    public AuthorizeResult(AuthorizeResponse response)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
    }

    internal AuthorizeResult(
        AuthorizeResponse response,
        IdentityServerOptions options,
        IUserSession userSession,
        IMessageStore<ErrorMessage> errorMessageStore,
        IServerUrls urls,
        ISystemClock clock)
        : this(response)
    {
        _options = options;
        _userSession = userSession;
        _errorMessageStore = errorMessageStore;
        _urls = urls;
        _clock = clock;
    }

    private IdentityServerOptions _options;
    private IUserSession _userSession;
    private IMessageStore<ErrorMessage> _errorMessageStore;
    private IServerUrls _urls;
    private ISystemClock _clock;

    private void Init(HttpContext context)
    {
        _options ??= context.RequestServices.GetRequiredService<IdentityServerOptions>();
        _userSession ??= context.RequestServices.GetRequiredService<IUserSession>();
        _errorMessageStore ??= context.RequestServices.GetRequiredService<IMessageStore<ErrorMessage>>();
        _urls = _urls ?? context.RequestServices.GetRequiredService<IServerUrls>();
        _clock ??= context.RequestServices.GetRequiredService<ISystemClock>();
    }

    public async Task ExecuteAsync(HttpContext context)
    {
        Init(context);

        if (Response.IsError)
        {
            await ProcessErrorAsync(context);
        }
        else
        {
            await ProcessResponseAsync(context);
        }
    }

    private async Task ProcessErrorAsync(HttpContext context)
    {
        // these are the conditions where we can send a response 
        // back directly to the client, otherwise we're only showing the error UI
        var isSafeError =
            Response.Error == OidcConstants.AuthorizeErrors.AccessDenied ||
            Response.Error == OidcConstants.AuthorizeErrors.AccountSelectionRequired ||
            Response.Error == OidcConstants.AuthorizeErrors.LoginRequired ||
            Response.Error == OidcConstants.AuthorizeErrors.ConsentRequired ||
            Response.Error == OidcConstants.AuthorizeErrors.InteractionRequired ||
            Response.Error == OidcConstants.AuthorizeErrors.TemporarilyUnavailable ||
            Response.Error == OidcConstants.AuthorizeErrors.UnmetAuthenticationRequirements;
        if (isSafeError)
        {
            // this scenario we can return back to the client
            await ProcessResponseAsync(context);
        }
        else
        {
            // we now know we must show error page
            await RedirectToErrorPageAsync(context);
        }
    }

    protected async Task ProcessResponseAsync(HttpContext context)
    {
        if (!Response.IsError)
        {
            // success response -- track client authorization for sign-out
            //_logger.LogDebug("Adding client {0} to client list cookie for subject {1}", request.ClientId, request.Subject.GetSubjectId());
            await _userSession.AddClientIdAsync(Response.Request.ClientId);
        }

        await RenderAuthorizeResponseAsync(context);
    }

    private async Task RenderAuthorizeResponseAsync(HttpContext context)
    {
        if (Response.Request.ResponseMode == OidcConstants.ResponseModes.Query ||
            Response.Request.ResponseMode == OidcConstants.ResponseModes.Fragment)
        {
            context.Response.SetNoCache();
            context.Response.Redirect(BuildRedirectUri());
        }
        else if (Response.Request.ResponseMode == OidcConstants.ResponseModes.FormPost)
        {
            context.Response.SetNoCache();
            AddSecurityHeaders(context);
            await context.Response.WriteHtmlAsync(GetFormPostHtml());
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
            context.Response.Headers.Add("Referrer-Policy", referrer_policy);
        }
    }

    private string BuildRedirectUri()
    {
        var uri = Response.RedirectUri;
        var query = Response.ToNameValueCollection(_options).ToQueryString();

        if (Response.Request.ResponseMode == OidcConstants.ResponseModes.Query)
        {
            uri = uri.AddQueryString(query);
        }
        else
        {
            uri = uri.AddHashFragment(query);
        }

        if (Response.IsError && !uri.Contains("#"))
        {
            // https://tools.ietf.org/html/draft-bradley-oauth-open-redirector-00
            uri += "#_=_";
        }

        return uri;
    }

    private const string FormPostHtml = "<html><head><meta http-equiv='X-UA-Compatible' content='IE=edge' /><base target='_self'/></head><body><form method='post' action='{uri}'>{body}<noscript><button>Click to continue</button></noscript></form><script>window.addEventListener('load', function(){document.forms[0].submit();});</script></body></html>";

    private string GetFormPostHtml()
    {
        var html = FormPostHtml;

        var url = Response.Request.RedirectUri;
        url = HtmlEncoder.Default.Encode(url);
        html = html.Replace("{uri}", url);
        html = html.Replace("{body}", Response.ToNameValueCollection(_options).ToFormPost());

        return html;
    }

    private async Task RedirectToErrorPageAsync(HttpContext context)
    {
        var errorModel = new ErrorMessage
        {
            RequestId = context.TraceIdentifier,
            Error = Response.Error,
            ErrorDescription = Response.ErrorDescription,
            UiLocales = Response.Request?.UiLocales,
            DisplayMode = Response.Request?.DisplayMode,
            ClientId = Response.Request?.ClientId
        };

        if (Response.RedirectUri != null && Response.Request?.ResponseMode != null)
        {
            // if we have a valid redirect uri, then include it to the error page
            errorModel.RedirectUri = BuildRedirectUri();
            errorModel.ResponseMode = Response.Request.ResponseMode;
        }

        var message = new Message<ErrorMessage>(errorModel, _clock.UtcNow.UtcDateTime);
        var id = await _errorMessageStore.WriteAsync(message);

        var errorUrl = _options.UserInteraction.ErrorUrl;

        var url = errorUrl.AddQueryString(_options.UserInteraction.ErrorIdParameter, id);
        context.Response.Redirect(_urls.GetAbsoluteUrl(url));
    }
}