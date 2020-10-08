// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityModel;
using IdentityServer.UnitTests.Common;
using IdentityServer4.Configuration;
using IdentityServer4.Endpoints.Results;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace IdentityServer.UnitTests.Endpoints.Results
{
    public class AuthorizeResultTests
    {
        private AuthorizeResult _subject;

        private AuthorizeResponse _response = new AuthorizeResponse();
        private IdentityServerOptions _options = new IdentityServerOptions();
        private MockUserSession _mockUserSession = new MockUserSession();
        private MockMessageStore<IdentityServer4.Models.ErrorMessage> _mockErrorMessageStore = new MockMessageStore<IdentityServer4.Models.ErrorMessage>();

        private DefaultHttpContext _context = new DefaultHttpContext();

        public AuthorizeResultTests()
        {
            _context.SetIdentityServerOrigin("https://server");
            _context.SetIdentityServerBasePath("/");
            _context.Response.Body = new MemoryStream();

            _options.UserInteraction.ErrorUrl = "~/error";
            _options.UserInteraction.ErrorIdParameter = "errorId";

            _subject = new AuthorizeResult(_response, _options, _mockUserSession, _mockErrorMessageStore, new StubClock());
        }

        [Fact]
        public async Task error_should_redirect_to_error_page_and_passs_info()
        {
            _response.Error = "some_error";

            await _subject.ExecuteAsync(_context);

            _mockErrorMessageStore.Messages.Count.Should().Be(1);
            _context.Response.StatusCode.Should().Be(302);
            var location = _context.Response.Headers["Location"].First();
            location.Should().StartWith("https://server/error");
            var query = QueryHelpers.ParseQuery(new Uri(location).Query);
            query["errorId"].First().Should().Be(_mockErrorMessageStore.Messages.First().Key);
        }

        [Theory]
        [InlineData(OidcConstants.AuthorizeErrors.AccountSelectionRequired)]
        [InlineData(OidcConstants.AuthorizeErrors.LoginRequired)]
        [InlineData(OidcConstants.AuthorizeErrors.ConsentRequired)]
        [InlineData(OidcConstants.AuthorizeErrors.InteractionRequired)]
        public async Task prompt_none_errors_should_return_to_client(string error)
        {
            _response.Error = error;
            _response.Request = new ValidatedAuthorizeRequest
            {
                ResponseMode = OidcConstants.ResponseModes.Query,
                RedirectUri = "http://client/callback",
                PromptModes = new[] { "none" }
            };

            await _subject.ExecuteAsync(_context);

            _mockUserSession.Clients.Count.Should().Be(0);
            _context.Response.StatusCode.Should().Be(302);
            var location = _context.Response.Headers["Location"].First();
            location.Should().StartWith("http://client/callback");
        }

        [Theory]
        [InlineData(OidcConstants.AuthorizeErrors.AccountSelectionRequired)]
        [InlineData(OidcConstants.AuthorizeErrors.LoginRequired)]
        [InlineData(OidcConstants.AuthorizeErrors.ConsentRequired)]
        [InlineData(OidcConstants.AuthorizeErrors.InteractionRequired)]
        public async Task prompt_none_errors_for_anonymous_users_should_include_session_state(string error)
        {
            _response.Error = error;
            _response.Request = new ValidatedAuthorizeRequest
            {
                ResponseMode = OidcConstants.ResponseModes.Query,
                RedirectUri = "http://client/callback",
                PromptModes = new[] { "none" },
            };
            _response.SessionState = "some_session_state";

            await _subject.ExecuteAsync(_context);

            _mockUserSession.Clients.Count.Should().Be(0);
            _context.Response.StatusCode.Should().Be(302);
            var location = _context.Response.Headers["Location"].First();
            location.Should().Contain("session_state=some_session_state");
        }

        [Fact]
        public async Task access_denied_should_return_to_client()
        {
            const string errorDescription = "some error description";

            _response.Error = OidcConstants.AuthorizeErrors.AccessDenied;
            _response.ErrorDescription = errorDescription;
            _response.Request = new ValidatedAuthorizeRequest
            {
                ResponseMode = OidcConstants.ResponseModes.Query,
                RedirectUri = "http://client/callback"
            };

            await _subject.ExecuteAsync(_context);

            _mockUserSession.Clients.Count.Should().Be(0);
            _context.Response.StatusCode.Should().Be(302);
            var location = _context.Response.Headers["Location"].First();
            location.Should().StartWith("http://client/callback");

            var queryString = new Uri(location).Query;
            var queryParams = QueryHelpers.ParseQuery(queryString);

            queryParams["error"].Should().Equal(OidcConstants.AuthorizeErrors.AccessDenied);
            queryParams["error_description"].Should().Equal(errorDescription);
        }

        [Fact]
        public async Task success_should_add_client_to_client_list()
        {
            _response.Request = new ValidatedAuthorizeRequest
            {
                ClientId = "client",
                ResponseMode = OidcConstants.ResponseModes.Query,
                RedirectUri = "http://client/callback"
            };

            await _subject.ExecuteAsync(_context);

            _mockUserSession.Clients.Should().Contain("client");
        }

        [Fact]
        public async Task query_mode_should_pass_results_in_query()
        {
            _response.Request = new ValidatedAuthorizeRequest
            {
                ClientId = "client",
                ResponseMode = OidcConstants.ResponseModes.Query,
                RedirectUri = "http://client/callback",
                State = "state"
            };

            await _subject.ExecuteAsync(_context);

            _context.Response.StatusCode.Should().Be(302);
            _context.Response.Headers["Cache-Control"].First().Should().Contain("no-store");
            _context.Response.Headers["Cache-Control"].First().Should().Contain("no-cache");
            _context.Response.Headers["Cache-Control"].First().Should().Contain("max-age=0");
            var location = _context.Response.Headers["Location"].First();
            location.Should().StartWith("http://client/callback");
            location.Should().Contain("?state=state");
        }

        [Fact]
        public async Task fragment_mode_should_pass_results_in_fragment()
        {
            _response.Request = new ValidatedAuthorizeRequest
            {
                ClientId = "client",
                ResponseMode = OidcConstants.ResponseModes.Fragment,
                RedirectUri = "http://client/callback",
                State = "state"
            };

            await _subject.ExecuteAsync(_context);

            _context.Response.StatusCode.Should().Be(302);
            _context.Response.Headers["Cache-Control"].First().Should().Contain("no-store");
            _context.Response.Headers["Cache-Control"].First().Should().Contain("no-cache");
            _context.Response.Headers["Cache-Control"].First().Should().Contain("max-age=0");
            var location = _context.Response.Headers["Location"].First();
            location.Should().StartWith("http://client/callback");
            location.Should().Contain("#state=state");
        }

        [Fact]
        public async Task form_post_mode_should_pass_results_in_body()
        {
            _response.Request = new ValidatedAuthorizeRequest
            {
                ClientId = "client",
                ResponseMode = OidcConstants.ResponseModes.FormPost,
                RedirectUri = "http://client/callback",
                State = "state"
            };

            await _subject.ExecuteAsync(_context);

            _context.Response.StatusCode.Should().Be(200);
            _context.Response.ContentType.Should().StartWith("text/html");
            _context.Response.Headers["Cache-Control"].First().Should().Contain("no-store");
            _context.Response.Headers["Cache-Control"].First().Should().Contain("no-cache");
            _context.Response.Headers["Cache-Control"].First().Should().Contain("max-age=0");
            _context.Response.Headers["Content-Security-Policy"].First().Should().Contain("default-src 'none';");
            _context.Response.Headers["Content-Security-Policy"].First().Should().Contain("script-src 'sha256-orD0/VhH8hLqrLxKHD/HUEMdwqX6/0ve7c5hspX5VJ8='");
            _context.Response.Headers["X-Content-Security-Policy"].First().Should().Contain("default-src 'none';");
            _context.Response.Headers["X-Content-Security-Policy"].First().Should().Contain("script-src 'sha256-orD0/VhH8hLqrLxKHD/HUEMdwqX6/0ve7c5hspX5VJ8='");
            _context.Response.Body.Seek(0, SeekOrigin.Begin);
            using (var rdr = new StreamReader(_context.Response.Body))
            {
                var html = rdr.ReadToEnd();
                html.Should().Contain("<base target='_self'/>");
                html.Should().Contain("<form method='post' action='http://client/callback'>");
                html.Should().Contain("<input type='hidden' name='state' value='state' />");
            }
        }

        [Fact]
        public async Task form_post_mode_should_add_unsafe_inline_for_csp_level_1()
        {
            _response.Request = new ValidatedAuthorizeRequest
            {
                ClientId = "client",
                ResponseMode = OidcConstants.ResponseModes.FormPost,
                RedirectUri = "http://client/callback",
                State = "state"
            };

            _options.Csp.Level = CspLevel.One;

            await _subject.ExecuteAsync(_context);

            _context.Response.Headers["Content-Security-Policy"].First().Should().Contain("script-src 'unsafe-inline' 'sha256-orD0/VhH8hLqrLxKHD/HUEMdwqX6/0ve7c5hspX5VJ8='");
            _context.Response.Headers["X-Content-Security-Policy"].First().Should().Contain("script-src 'unsafe-inline' 'sha256-orD0/VhH8hLqrLxKHD/HUEMdwqX6/0ve7c5hspX5VJ8='");
        }

        [Fact]
        public async Task form_post_mode_should_not_add_deprecated_header_when_it_is_disabled()
        {
            _response.Request = new ValidatedAuthorizeRequest
            {
                ClientId = "client",
                ResponseMode = OidcConstants.ResponseModes.FormPost,
                RedirectUri = "http://client/callback",
                State = "state"
            };

            _options.Csp.AddDeprecatedHeader = false;

            await _subject.ExecuteAsync(_context);

            _context.Response.Headers["Content-Security-Policy"].First().Should().Contain("script-src 'sha256-orD0/VhH8hLqrLxKHD/HUEMdwqX6/0ve7c5hspX5VJ8='");
            _context.Response.Headers["X-Content-Security-Policy"].Should().BeEmpty();
        }
    }
}
