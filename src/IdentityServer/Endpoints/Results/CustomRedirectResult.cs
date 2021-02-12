// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.Validation;
using Microsoft.AspNetCore.Http;
using Duende.IdentityServer.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Duende.IdentityServer.Endpoints.Results
{
    /// <summary>
    /// Result for a custom redirect
    /// </summary>
    /// <seealso cref="IEndpointResult" />
    public class CustomRedirectResult : IEndpointResult
    {
        private readonly ValidatedAuthorizeRequest _request;
        private readonly string _url;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomRedirectResult"/> class.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="url">The URL.</param>
        /// <exception cref="System.ArgumentNullException">
        /// request
        /// or
        /// url
        /// </exception>
        public CustomRedirectResult(ValidatedAuthorizeRequest request, string url)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (url.IsMissing()) throw new ArgumentNullException(nameof(url));

            _request = request;
            _url = url;
        }

        internal CustomRedirectResult(
            ValidatedAuthorizeRequest request,
            string url,
            IdentityServerOptions options) 
            : this(request, url)
        {
            _options = options;
        }

        private IdentityServerOptions _options;

        private void Init(HttpContext context)
        {
            _options = _options ?? context.RequestServices.GetRequiredService<IdentityServerOptions>();
        }

        /// <summary>
        /// Executes the result.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns></returns>
        public Task ExecuteAsync(HttpContext context)
        {
            Init(context);

            var returnUrl = context.GetIdentityServerBasePath().EnsureTrailingSlash() + Constants.ProtocolRoutePaths.AuthorizeCallback;
            returnUrl = returnUrl.AddQueryString(_request.Raw.ToQueryString());

            if (!_url.IsLocalUrl())
            {
                // this converts the relative redirect path to an absolute one if we're 
                // redirecting to a different server
                returnUrl = context.GetIdentityServerBaseUrl().EnsureTrailingSlash() + returnUrl.RemoveLeadingSlash();
            }

            var url = _url.AddQueryString(_options.UserInteraction.CustomRedirectReturnUrlParameter, returnUrl);
            context.Response.RedirectToAbsoluteUrl(url);

            return Task.CompletedTask;
        }
    }
}