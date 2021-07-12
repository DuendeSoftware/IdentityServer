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

namespace Duende.IdentityServer.Endpoints.Results
{
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
                string frameSources = null;
                var origins = _result.FrontChannelLogoutUrls?.Select(x => x.GetOrigin());
                if (origins != null && origins.Any())
                {
                    frameSources = origins.Distinct().Aggregate((x, y) => $"{x} {y}");
                }

                // the hash matches the embedded style element being used below
                context.Response.AddStyleCspHeaders(_options.Csp, "sha256-u+OupXgfekP+x/f6rMdoEAspPCYUtca912isERnoEjY=", frameSources);
            }
        }

        private string GetHtml()
        {
            string framesHtml = null;

            if (_result.FrontChannelLogoutUrls != null && _result.FrontChannelLogoutUrls.Any())
            {
                var frameUrls = _result.FrontChannelLogoutUrls.Select(url => $"<iframe src='{HtmlEncoder.Default.Encode(url)}'></iframe>"); 
                framesHtml = frameUrls.Aggregate((x, y) => x + y);
            }

            return $"<!DOCTYPE html><html><style>iframe{{display:none;width:0;height:0;}}</style><body>{framesHtml}</body></html>";
        }
    }
}
