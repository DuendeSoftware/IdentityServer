// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.Validation;
using Duende.IdentityServer.Extensions;
using System.Text.Encodings.Web;
using System.Text;

namespace Duende.IdentityServer.Endpoints.Results;

internal class EndSessionCallbackResult : IEndpointResult
{
    private readonly EndSessionCallbackValidationResult _result;

    public EndSessionCallbackResult(EndSessionCallbackValidationResult result)
    {
        _result = result ?? throw new ArgumentNullException(nameof(result));
    }

    internal EndSessionCallbackResult(
        EndSessionCallbackValidationResult result,
        IdentityServerOptions options)
        : this(result)
    {
        _options = options;
    }

    private IdentityServerOptions _options;

    private void Init(HttpContext context)
    {
        _options = _options ?? context.RequestServices.GetRequiredService<IdentityServerOptions>();
    }

    public async Task ExecuteAsync(HttpContext context)
    {
        Init(context);

        if (_result.IsError)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
        else
        {
            context.Response.SetNoCache();
            AddCspHeaders(context);

            var html = GetHtml();
            await context.Response.WriteHtmlAsync(html);
        }
    }

    private void AddCspHeaders(HttpContext context)
    {
        if (_options.Authentication.RequireCspFrameSrcForSignout)
        {
            var sb = new StringBuilder();
            var origins = _result.FrontChannelLogoutUrls?.Select(x => x.GetOrigin());
            if (origins != null)
            {
                foreach (var origin in origins.Distinct())
                {
                    sb.Append(origin);
                    if (sb.Length > 0) sb.Append(" ");
                }
            }

            // the hash matches the embedded style element being used below
            context.Response.AddStyleCspHeaders(_options.Csp, IdentityServerConstants.ContentSecurityPolicyHashes.EndSessionStyle, sb.ToString());
        }
    }

    private string GetHtml()
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><style>iframe{{display:none;width:0;height:0;}}</style><body>");

        if (_result.FrontChannelLogoutUrls != null)
        {
            foreach (var url in _result.FrontChannelLogoutUrls)
            {
                sb.AppendFormat("<iframe loading='eager' allow='' src='{0}'></iframe>", HtmlEncoder.Default.Encode(url));
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}