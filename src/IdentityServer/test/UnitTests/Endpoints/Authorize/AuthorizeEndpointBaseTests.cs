// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Specialized;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityServer.UnitTests.Common;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Endpoints;
using IdentityServer4.Endpoints.Results;
using IdentityServer4.Hosting;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IdentityServer.UnitTests.Endpoints.Authorize
{
    public class AuthorizeEndpointBaseTests
    {
        private const string Category = "Authorize Endpoint";

        private HttpContext _context;

        private TestEventService _fakeEventService = new TestEventService();

        private ILogger<TestAuthorizeEndpoint> _fakeLogger = TestLogger.Create<TestAuthorizeEndpoint>();

        private IdentityServerOptions _options = new IdentityServerOptions();

        private MockUserSession _mockUserSession = new MockUserSession();

        private NameValueCollection _params = new NameValueCollection();

        private StubAuthorizeRequestValidator _stubAuthorizeRequestValidator = new StubAuthorizeRequestValidator();

        private StubAuthorizeResponseGenerator _stubAuthorizeResponseGenerator = new StubAuthorizeResponseGenerator();

        private StubAuthorizeInteractionResponseGenerator _stubInteractionGenerator = new StubAuthorizeInteractionResponseGenerator();

        private TestAuthorizeEndpoint _subject;

        private ClaimsPrincipal _user = new IdentityServerUser("bob").CreatePrincipal();

        private ValidatedAuthorizeRequest _validatedAuthorizeRequest;

        public AuthorizeEndpointBaseTests()
        {
            Init();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task error_resurect_with_prompt_none_should_include_session_state_in_response()
        {
            _params.Add("prompt", "none");
            _stubAuthorizeRequestValidator.Result.ValidatedRequest.IsOpenIdRequest = true;
            _stubAuthorizeRequestValidator.Result.ValidatedRequest.ClientId = "client";
            _stubAuthorizeRequestValidator.Result.ValidatedRequest.SessionId = "some_session";
            _stubAuthorizeRequestValidator.Result.ValidatedRequest.RedirectUri = "http://redirect";
            _stubAuthorizeRequestValidator.Result.IsError = true;
            _stubAuthorizeRequestValidator.Result.Error = "login_required";

            var result = await _subject.ProcessAuthorizeRequestAsync(_params, _user, null);

            result.Should().BeOfType<AuthorizeResult>();
            ((AuthorizeResult)result).Response.IsError.Should().BeTrue();
            ((AuthorizeResult)result).Response.SessionState.Should().NotBeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authorize_request_validation_produces_error_should_display_error_page()
        {
            _stubAuthorizeRequestValidator.Result.IsError = true;
            _stubAuthorizeRequestValidator.Result.Error = "some_error";

            var result = await _subject.ProcessAuthorizeRequestAsync(_params, _user, null);

            result.Should().BeOfType<AuthorizeResult>();
            ((AuthorizeResult)result).Response.IsError.Should().BeTrue();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task interaction_generator_consent_produces_consent_should_show_consent_page()
        {
            _stubInteractionGenerator.Response.IsConsent = true;

            var result = await _subject.ProcessAuthorizeRequestAsync(_params, _user, null);

            result.Should().BeOfType<ConsentPageResult>();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task interaction_produces_error_should_show_error_page()
        {
            _stubInteractionGenerator.Response.Error = "error";

            var result = await _subject.ProcessAuthorizeRequestAsync(_params, _user, null);

            result.Should().BeOfType<AuthorizeResult>();
            ((AuthorizeResult)result).Response.IsError.Should().BeTrue();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task interaction_produces_error_should_show_error_page_with_error_description_if_present()
        {
            var errorDescription = "some error description";

             _stubInteractionGenerator.Response.Error = "error";
            _stubInteractionGenerator.Response.ErrorDescription = errorDescription;

             var result = await _subject.ProcessAuthorizeRequestAsync(_params, _user, null);

             result.Should().BeOfType<AuthorizeResult>();
            var authorizeResult = ((AuthorizeResult)result);
            authorizeResult.Response.IsError.Should().BeTrue();
            authorizeResult.Response.ErrorDescription.Should().Be(errorDescription);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task interaction_produces_login_result_should_trigger_login()
        {
            _stubInteractionGenerator.Response.IsLogin = true;

            var result = await _subject.ProcessAuthorizeRequestAsync(_params, _user, null);

            result.Should().BeOfType<LoginPageResult>();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task ProcessAuthorizeRequestAsync_custom_interaction_redirect_result_should_issue_redirect()
        {
            _mockUserSession.User = _user;
            _stubInteractionGenerator.Response.RedirectUrl = "http://foo.com";

            var result = await _subject.ProcessAuthorizeRequestAsync(_params, _user, null);

            result.Should().BeOfType<CustomRedirectResult>();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task successful_authorization_request_should_generate_authorize_result()
        {
            var result = await _subject.ProcessAuthorizeRequestAsync(_params, _user, null);

            result.Should().BeOfType<AuthorizeResult>();
        }

        internal void Init()
        {
            _context = new MockHttpContextAccessor().HttpContext;

            _validatedAuthorizeRequest = new ValidatedAuthorizeRequest()
            {
                RedirectUri = "http://client/callback",
                State = "123",
                ResponseMode = "fragment",
                ClientId = "client",
                Client = new Client
                {
                    ClientId = "client",
                    ClientName = "Test Client"
                },
                Raw = _params,
                Subject = _user
            };
            _stubAuthorizeResponseGenerator.Response.Request = _validatedAuthorizeRequest;

            _stubAuthorizeRequestValidator.Result = new AuthorizeRequestValidationResult(_validatedAuthorizeRequest);

            _subject = new TestAuthorizeEndpoint(
                _fakeEventService,
                _fakeLogger,
                _options,
                _stubAuthorizeRequestValidator,
                _stubInteractionGenerator,
                _stubAuthorizeResponseGenerator,
                _mockUserSession);
        }

        internal class TestAuthorizeEndpoint : AuthorizeEndpointBase
        {
            public TestAuthorizeEndpoint(
              IEventService events,
              ILogger<TestAuthorizeEndpoint> logger,
              IdentityServerOptions options,
              IAuthorizeRequestValidator validator,
              IAuthorizeInteractionResponseGenerator interactionGenerator,
              IAuthorizeResponseGenerator authorizeResponseGenerator,
              IUserSession userSession)
            : base(events, logger, options, validator, interactionGenerator, authorizeResponseGenerator, userSession)
            {
            }

            public override Task<IEndpointResult> ProcessAsync(HttpContext context)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
