// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityServer.UnitTests.Common;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Xunit;

namespace IdentityServer.UnitTests.Services.Default
{
    public class DefaultUserSessionTests
    {
        private DefaultUserSession _subject;
        private MockHttpContextAccessor _mockHttpContext = new MockHttpContextAccessor();
        private MockAuthenticationHandlerProvider _mockAuthenticationHandlerProvider = new MockAuthenticationHandlerProvider();
        private MockAuthenticationHandler _mockAuthenticationHandler = new MockAuthenticationHandler();

        private IdentityServerOptions _options = new IdentityServerOptions();
        private ClaimsPrincipal _user;
        private AuthenticationProperties _props = new AuthenticationProperties();

        public DefaultUserSessionTests()
        {
            _mockAuthenticationHandlerProvider.Handler = _mockAuthenticationHandler;

            _user = new IdentityServerUser("123").CreatePrincipal();
            _subject = new DefaultUserSession(
                _mockHttpContext, 
                _mockAuthenticationHandlerProvider,
                _options,
                new StubClock(), 
                TestLogger.Create<DefaultUserSession>());
        }

        [Fact]
        public async Task CreateSessionId_when_user_is_anonymous_should_generate_new_sid()
        {
            await _subject.CreateSessionIdAsync(_user, _props);

            _props.GetSessionId().Should().NotBeNull();
        }

        [Fact]
        public async Task CreateSessionId_when_user_is_authenticated_should_not_generate_new_sid()
        {
            // this test is needed to allow same session id when cookie is slid
            // IOW, if UI layer passes in same properties dictionary, then we assume it's the same user

            _props.SetSessionId("999");
            _mockAuthenticationHandler.Result = AuthenticateResult.Success(new AuthenticationTicket(_user, _props, "scheme"));

            await _subject.CreateSessionIdAsync(_user, _props);

            _props.GetSessionId().Should().NotBeNull();
            _props.GetSessionId().Should().Be("999");
        }

        [Fact]
        public async Task CreateSessionId_when_props_does_not_contain_key_should_generate_new_sid()
        {
            _mockAuthenticationHandler.Result = AuthenticateResult.Success(new AuthenticationTicket(_user, _props, "scheme"));

            _props.GetSessionId().Should().BeNull();

            await _subject.CreateSessionIdAsync(_user, _props);

            _props.GetSessionId().Should().NotBeNull();
        }

        [Fact]
        public async Task CreateSessionId_when_user_is_authenticated_but_different_sub_should_create_new_sid()
        {
            _props.SetSessionId("999");
            _mockAuthenticationHandler.Result = AuthenticateResult.Success(new AuthenticationTicket(_user, _props, "scheme"));

            await _subject.CreateSessionIdAsync(new IdentityServerUser("alice").CreatePrincipal(), _props);

            _props.GetSessionId().Should().NotBeNull();
            _props.GetSessionId().Should().NotBe("999");
        }

        [Fact]
        public async Task CreateSessionId_should_issue_session_id_cookie()
        {
            await _subject.CreateSessionIdAsync(_user, _props);

            var cookieContainer = new CookieContainer();
            var cookies = _mockHttpContext.HttpContext.Response.Headers.Where(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value);
            cookieContainer.SetCookies(new Uri("http://server"), string.Join(",", cookies));
            _mockHttpContext.HttpContext.Response.Headers.Clear();

            var cookie = cookieContainer.GetCookies(new Uri("http://server")).Cast<Cookie>().Where(x => x.Name == _options.Authentication.CheckSessionCookieName).FirstOrDefault();
            cookie.Value.Should().Be(_props.GetSessionId());
        }

        [Fact]
        public async Task EnsureSessionIdCookieAsync_should_add_cookie()
        {
            _props.SetSessionId("999");
            _mockAuthenticationHandler.Result = AuthenticateResult.Success(new AuthenticationTicket(_user, _props, "scheme"));

            await _subject.EnsureSessionIdCookieAsync();

            var cookieContainer = new CookieContainer();
            var cookies = _mockHttpContext.HttpContext.Response.Headers.Where(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value);
            cookieContainer.SetCookies(new Uri("http://server"), string.Join(",", cookies));
            _mockHttpContext.HttpContext.Response.Headers.Clear();

            var cookie = cookieContainer.GetCookies(new Uri("http://server")).Cast<Cookie>().Where(x => x.Name == _options.Authentication.CheckSessionCookieName).FirstOrDefault();
            cookie.Value.Should().Be("999");
        }

        [Fact]
        public async Task EnsureSessionIdCookieAsync_should_not_add_cookie_if_no_sid()
        {
            await _subject.EnsureSessionIdCookieAsync();

            var cookieContainer = new CookieContainer();
            var cookies = _mockHttpContext.HttpContext.Response.Headers.Where(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value);
            cookieContainer.SetCookies(new Uri("http://server"), string.Join(",", cookies));
            _mockHttpContext.HttpContext.Response.Headers.Clear();

            var cookie = cookieContainer.GetCookies(new Uri("http://server")).Cast<Cookie>().Where(x => x.Name == _options.Authentication.CheckSessionCookieName).FirstOrDefault();
            cookie.Should().BeNull();
        }

        [Fact]
        public async Task RemoveSessionIdCookie_should_remove_cookie()
        {
            _props.SetSessionId("999");
            _mockAuthenticationHandler.Result = AuthenticateResult.Success(new AuthenticationTicket(_user, _props, "scheme"));

            await _subject.EnsureSessionIdCookieAsync();

            var cookieContainer = new CookieContainer();
            var cookies = _mockHttpContext.HttpContext.Response.Headers.Where(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value);
            cookieContainer.SetCookies(new Uri("http://server"), string.Join(",", cookies));
            _mockHttpContext.HttpContext.Response.Headers.Clear();

            string cookie = cookieContainer.GetCookieHeader(new Uri("http://server"));
            _mockHttpContext.HttpContext.Request.Headers.Add("Cookie", cookie);

            await _subject.RemoveSessionIdCookieAsync();

            cookies = _mockHttpContext.HttpContext.Response.Headers.Where(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value);
            cookieContainer.SetCookies(new Uri("http://server"), string.Join(",", cookies));

            var query = cookieContainer.GetCookies(new Uri("http://server")).Cast<Cookie>().Where(x => x.Name == _options.Authentication.CheckSessionCookieName);
            query.Count().Should().Be(0);
        }

        [Fact]
        public async Task GetCurrentSessionIdAsync_when_user_is_authenticated_should_return_sid()
        {
            _props.SetSessionId("999");
            _mockAuthenticationHandler.Result = AuthenticateResult.Success(new AuthenticationTicket(_user, _props, "scheme"));

            var sid = await _subject.GetSessionIdAsync();
            sid.Should().Be("999");
        }

        [Fact]
        public async Task GetCurrentSessionIdAsync_when_user_is_anonymous_should_return_null()
        {
            var sid = await _subject.GetSessionIdAsync();
            sid.Should().BeNull();
        }

        [Fact]
        public async Task adding_client_should_set_item_in_cookie_properties()
        {
            _mockAuthenticationHandler.Result = AuthenticateResult.Success(new AuthenticationTicket(_user, _props, "scheme"));

            _props.Items.Count.Should().Be(0);
            await _subject.AddClientIdAsync("client");
            _props.Items.Count.Should().Be(1);
        }

        [Fact]
        public async Task when_authenticated_GetIdentityServerUserAsync_should_should_return_authenticated_user()
        {
            _mockAuthenticationHandler.Result = AuthenticateResult.Success(new AuthenticationTicket(_user, _props, "scheme"));

            var user = await _subject.GetUserAsync();
            user.GetSubjectId().Should().Be("123");
        }

        [Fact]
        public async Task when_anonymous_GetIdentityServerUserAsync_should_should_return_null()
        {
            var user = await _subject.GetUserAsync();
            user.Should().BeNull();
        }

        [Fact]
        public async Task corrupt_properties_entry_should_clear_entry()
        {
            _mockAuthenticationHandler.Result = AuthenticateResult.Success(new AuthenticationTicket(_user, _props, "scheme"));

            await _subject.AddClientIdAsync("client");
            var item = _props.Items.First();
            _props.Items[item.Key] = "junk";

            var clients = await _subject.GetClientListAsync();
            clients.Should().BeEmpty();
            _props.Items.Count.Should().Be(0);
        }

        [Fact]
        public async Task adding_client_should_be_able_to_read_client()
        {
            _mockAuthenticationHandler.Result = AuthenticateResult.Success(new AuthenticationTicket(_user, _props, "scheme"));

            await _subject.AddClientIdAsync("client");
            var clients = await _subject.GetClientListAsync();
            clients.Should().Contain(new string[] { "client" });
        }

        [Fact]
        public async Task adding_clients_should_be_able_to_read_clients()
        {
            _mockAuthenticationHandler.Result = AuthenticateResult.Success(new AuthenticationTicket(_user, _props, "scheme"));

            await _subject.AddClientIdAsync("client1");
            await _subject.AddClientIdAsync("client2");
            var clients = await _subject.GetClientListAsync();
            clients.Should().Contain(new string[] { "client2", "client1" });
        }
    }
}
