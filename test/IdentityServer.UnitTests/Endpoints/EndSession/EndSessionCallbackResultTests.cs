// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Endpoints.Results;
using Duende.IdentityServer.Validation;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace UnitTests.Endpoints.EndSession;

public class EndSessionCallbackResultTests
{
    private const string Category = "End Session Callback Result";

    private readonly EndSessionCallbackValidationResult _validationResult;
    private readonly IdentityServerOptions _options;
    private readonly EndSessionCallbackResultGenerator _subject;

    public EndSessionCallbackResultTests()
    {
        _validationResult = new EndSessionCallbackValidationResult()
        {
            IsError = false,
        };
        _options = new IdentityServerOptions();
        _subject = new EndSessionCallbackResultGenerator(_options);
    }

    [Fact]
    public async Task default_options_should_emit_frame_src_csp_headers()
    {
        _validationResult.FrontChannelLogoutUrls = new[] { "http://foo" };

        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";

        await _subject.ExecuteAsync(new EndSessionCallbackResult(_validationResult), ctx);

        ctx.Response.Headers["Content-Security-Policy"].First().Should().Contain("frame-src http://foo");
    }

    [Fact]
    public async Task relax_csp_options_should_prevent_frame_src_csp_headers()
    {
        _options.Authentication.RequireCspFrameSrcForSignout = false;
        _validationResult.FrontChannelLogoutUrls = new[] { "http://foo" };

        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";

        await _subject.ExecuteAsync(new EndSessionCallbackResult(_validationResult), ctx);

        ctx.Response.Headers["Content-Security-Policy"].FirstOrDefault().Should().BeNull();
    }
}