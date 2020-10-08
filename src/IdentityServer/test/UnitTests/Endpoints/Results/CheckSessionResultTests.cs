// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityServer4.Configuration;
using IdentityServer4.Endpoints.Results;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace IdentityServer.UnitTests.Endpoints.Results
{
    public class CheckSessionResultTests
    {
        private CheckSessionResult _subject;

        private IdentityServerOptions _options = new IdentityServerOptions();

        private DefaultHttpContext _context = new DefaultHttpContext();

        public CheckSessionResultTests()
        {
            _context.SetIdentityServerOrigin("https://server");
            _context.SetIdentityServerBasePath("/");
            _context.Response.Body = new MemoryStream();

            _options.Authentication.CheckSessionCookieName = "foobar";

            _subject = new CheckSessionResult(_options);
        }

        [Fact]
        public async Task should_pass_results_in_body()
        {
            await _subject.ExecuteAsync(_context);

            _context.Response.StatusCode.Should().Be(200);
            _context.Response.ContentType.Should().StartWith("text/html");
            _context.Response.Headers["Content-Security-Policy"].First().Should().Contain("default-src 'none';");
            _context.Response.Headers["Content-Security-Policy"].First().Should().Contain("script-src 'sha256-fa5rxHhZ799izGRP38+h4ud5QXNT0SFaFlh4eqDumBI='");
            _context.Response.Headers["X-Content-Security-Policy"].First().Should().Contain("default-src 'none';");
            _context.Response.Headers["X-Content-Security-Policy"].First().Should().Contain("script-src 'sha256-fa5rxHhZ799izGRP38+h4ud5QXNT0SFaFlh4eqDumBI='");
            _context.Response.Body.Seek(0, SeekOrigin.Begin);
            using (var rdr = new StreamReader(_context.Response.Body))
            {
                var html = rdr.ReadToEnd();
                html.Should().Contain("<script id='cookie-name' type='application/json'>foobar</script>");
            }
        }

        [Fact]
        public async Task form_post_mode_should_add_unsafe_inline_for_csp_level_1()
        {
            _options.Csp.Level = CspLevel.One;

            await _subject.ExecuteAsync(_context);

            _context.Response.Headers["Content-Security-Policy"].First().Should().Contain("script-src 'unsafe-inline' 'sha256-fa5rxHhZ799izGRP38+h4ud5QXNT0SFaFlh4eqDumBI='");
            _context.Response.Headers["X-Content-Security-Policy"].First().Should().Contain("script-src 'unsafe-inline' 'sha256-fa5rxHhZ799izGRP38+h4ud5QXNT0SFaFlh4eqDumBI='");
        }

        [Fact]
        public async Task form_post_mode_should_not_add_deprecated_header_when_it_is_disabled()
        {
            _options.Csp.AddDeprecatedHeader = false;

            await _subject.ExecuteAsync(_context);

            _context.Response.Headers["Content-Security-Policy"].First().Should().Contain("script-src 'sha256-fa5rxHhZ799izGRP38+h4ud5QXNT0SFaFlh4eqDumBI='");
            _context.Response.Headers["X-Content-Security-Policy"].Should().BeEmpty();
        }

        [Theory]
        [InlineData("foobar")]
        [InlineData("morefoobar")]

        public async Task can_change_cached_cookiename(string cookieName)
        {
            _options.Authentication.CheckSessionCookieName = cookieName;
            await _subject.ExecuteAsync(_context);
            _context.Response.Body.Seek(0, SeekOrigin.Begin);
            using (var rdr = new StreamReader(_context.Response.Body))
            {
                var html = rdr.ReadToEnd();
                html.Should().Contain($"<script id='cookie-name' type='application/json'>{cookieName}</script>");
            }
        }
    }
}
