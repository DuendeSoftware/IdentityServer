// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using Xunit;

namespace UnitTests.Services.Default
{
    public class OidcReturnUrlParserTests
    {
        private OidcReturnUrlParser _subject;

        IdentityServerOptions _options = new IdentityServerOptions();
        DefaultHttpContext _httpContext = new DefaultHttpContext();

        public OidcReturnUrlParserTests()
        {
            _httpContext.Request.Scheme = "https";
            _httpContext.Request.Host = new HostString("server");

            _subject = new OidcReturnUrlParser(
                _options,
                null, null, 
                new HttpContextAccessor { HttpContext = _httpContext }, 
                new LoggerFactory().CreateLogger<OidcReturnUrlParser>());
        }

        [Theory]
        [InlineData("/connect/authorize")]
        [InlineData("/connect/authorize?foo=f1&bar=b1")]
        [InlineData("/connect/authorize/callback")]
        [InlineData("/connect/authorize/callback?foo=f1&bar=b1")]
        [InlineData("/foo/connect/authorize")]
        [InlineData("/foo/connect/authorize/callback")]
        public void IsValidReturnUrl_accepts_authorize_or_authorizecallback(string url)
        {
            var valid = _subject.IsValidReturnUrl(url);
            valid.Should().BeTrue();
        }
        
        [Theory]
        [InlineData(default(string))]
        [InlineData("")]
        [InlineData("/")]
        [InlineData("/path")]
        [InlineData("//connect/authorize")]
        [InlineData("/connect/authorizex")]
        [InlineData("/connect")]
        [InlineData("/connect/token")]
        [InlineData("/authorize")]
        [InlineData("/foo?/connect/authorize")]
        [InlineData("/foo#/connect/authorize")]
        [InlineData("/foo?#/connect/authorize")]
        [InlineData("/foo#?/connect/authorize")]
        [InlineData("//server/connect/authorize")]
        public void IsValidReturnUrl_rejects_non_authorize_or_authorizecallback(string url)
        {
            var valid = _subject.IsValidReturnUrl(url);
            valid.Should().BeFalse();
        }

        [Theory]
        [InlineData("https://server/connect/authorize")]
        [InlineData("HTTPS://server/connect/authorize")]
        [InlineData("https://SERVER/connect/authorize")]
        [InlineData("https://server/foo/connect/authorize")]
        public void IsValidReturnUrl_accepts_urls_with_current_host(string url)
        {
            _options.UserInteraction.AllowOriginInReturnUrl = true;
            var valid = _subject.IsValidReturnUrl(url);
            valid.Should().BeTrue();
        }

        [Fact]
        public void IsValidReturnUrl_when_AllowHostInReturnUrl_disabled_rejects_urls_with_current_host()
        {
            _options.UserInteraction.AllowOriginInReturnUrl = false;
            var valid = _subject.IsValidReturnUrl("https://server/connect/authorize");
            valid.Should().BeFalse();
        }

        [Theory]
        [InlineData("http://server/connect/authorize")]
        [InlineData("https:\\/server/connect/authorize")]
        [InlineData("https:\\\\server/connect/authorize")]
        [InlineData("https://foo/connect/authorize")]
        [InlineData("https://serverhttps://server/connect/authorize")]
        [InlineData("https://serverfoo/connect/authorize")]
        [InlineData("https://server//foo/connect/authorize")]
        [InlineData("https://server:443/connect/authorize")]
        public void IsValidReturnUrl_rejects_urls_with_incorrect_current_host(string url)
        {
            _options.UserInteraction.AllowOriginInReturnUrl = true;
            var valid = _subject.IsValidReturnUrl(url);
            valid.Should().BeFalse();
        }


        [Fact]
        public void IsValidReturnUrl_accepts_urls_with_unicode()
        {
            _options.UserInteraction.AllowOriginInReturnUrl = true;
            _httpContext.Request.Host = new HostString("грант.рф");

            var valid = _subject.IsValidReturnUrl("https://xn--80af5akm.xn--p1ai/connect/authorize");
            valid.Should().BeTrue();
        }

        [Theory]
        [InlineData("https://server:443/connect/authorize")]
        [InlineData("HTTPS://server:443/connect/authorize")]
        [InlineData("https://SERVER:443/connect/authorize")]
        public void IsValidReturnUrl_accepts_urls_with_current_port(string url)
        {
            _options.UserInteraction.AllowOriginInReturnUrl = true;
            _httpContext.Request.Host = new HostString("server:443");

            var valid = _subject.IsValidReturnUrl(url);
            valid.Should().BeTrue();
        }

        [Theory]
        [InlineData("https://server/connect/authorize")]
        [InlineData("https://server:80/connect/authorize")]
        [InlineData("https://server:4/connect/authorize")]
        [InlineData("https://foo:443/connect/authorize")]
        [InlineData("https://server:4433/connect/authorize")]
        [InlineData("https://server:443https://server:443/connect/authorize")]
        [InlineData("https://serverfoo:443/connect/authorize")]
        [InlineData("https://server:443foo/connect/authorize")]
        [InlineData("https://server:443//foo/connect/authorize")]
        public void IsValidReturnUrl_rejects_urls_with_incorrect_current_port(string url)
        {
            _options.UserInteraction.AllowOriginInReturnUrl = true;
            _httpContext.Request.Host = new HostString("server:443");
            
            var valid = _subject.IsValidReturnUrl(url);
            valid.Should().BeFalse();
        }
    }
}
