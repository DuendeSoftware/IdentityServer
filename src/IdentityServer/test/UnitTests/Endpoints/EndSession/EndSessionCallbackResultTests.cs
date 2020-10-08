// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityServer4.Configuration;
using IdentityServer4.Endpoints.Results;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace IdentityServer.UnitTests.Endpoints.EndSession
{
    public class EndSessionCallbackResultTests
    {
        private const string Category = "End Session Callback Result";

        private readonly EndSessionCallbackValidationResult _validationResult;
        private readonly IdentityServerOptions _options;
        private readonly EndSessionCallbackResult _subject;

        public EndSessionCallbackResultTests()
        {
            _validationResult = new EndSessionCallbackValidationResult()
            {
                IsError = false,
            };
            _options = new IdentityServerOptions();
            _subject = new EndSessionCallbackResult(_validationResult, _options);
        }

        [Fact]
        public async Task default_options_should_emit_frame_src_csp_headers()
        {
            _validationResult.FrontChannelLogoutUrls = new[] { "http://foo" };

            var ctx = new DefaultHttpContext();
            ctx.Request.Method = "GET";

            await _subject.ExecuteAsync(ctx);

            ctx.Response.Headers["Content-Security-Policy"].First().Should().Contain("frame-src http://foo");
        }

        [Fact]
        public async Task relax_csp_options_should_prevent_frame_src_csp_headers()
        {
            _options.Authentication.RequireCspFrameSrcForSignout = false;
            _validationResult.FrontChannelLogoutUrls = new[] { "http://foo" };

            var ctx = new DefaultHttpContext();
            ctx.Request.Method = "GET";

            await _subject.ExecuteAsync(ctx);

            ctx.Response.Headers["Content-Security-Policy"].FirstOrDefault().Should().BeNull();
        }
    }
}
