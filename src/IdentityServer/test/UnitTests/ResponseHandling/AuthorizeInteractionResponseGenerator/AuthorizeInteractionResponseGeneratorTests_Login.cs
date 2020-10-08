// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityModel;
using IdentityServer.UnitTests.Common;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Xunit;

namespace IdentityServer.UnitTests.ResponseHandling.AuthorizeInteractionResponseGenerator
{
    public class AuthorizeInteractionResponseGeneratorTests_Login
    {
        private IdentityServerOptions _options = new IdentityServerOptions();
        private IdentityServer4.ResponseHandling.AuthorizeInteractionResponseGenerator _subject;
        private MockConsentService _mockConsentService = new MockConsentService();
        private StubClock _clock = new StubClock();

        public AuthorizeInteractionResponseGeneratorTests_Login()
        {
            _subject = new IdentityServer4.ResponseHandling.AuthorizeInteractionResponseGenerator(
                _clock,
                TestLogger.Create<IdentityServer4.ResponseHandling.AuthorizeInteractionResponseGenerator>(),
                _mockConsentService,
                new MockProfileService());
        }

        [Fact]
        public async Task Anonymous_User_must_SignIn()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Subject = Principal.Anonymous
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task Authenticated_User_must_not_SignIn()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Client = new Client(),
                ValidatedResources = new ResourceValidationResult(),
                Subject = new IdentityServerUser("123")
                {
                    IdentityProvider = IdentityServerConstants.LocalIdentityProvider
                }.CreatePrincipal()
            };

            var result = await _subject.ProcessInteractionAsync(request);

            result.IsLogin.Should().BeFalse();
        }

        [Fact]
        public async Task Authenticated_User_with_allowed_current_Idp_must_not_SignIn()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Subject = new IdentityServerUser("123") {
                    IdentityProvider = IdentityServerConstants.LocalIdentityProvider
                }.CreatePrincipal(),
                Client = new Client 
                {
                    IdentityProviderRestrictions = new List<string> 
                    {
                        IdentityServerConstants.LocalIdentityProvider
                    }
                }
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeFalse();
        }

        [Fact]
        public async Task Authenticated_User_with_restricted_current_Idp_must_SignIn()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Subject = new IdentityServerUser("123")
                {
                    IdentityProvider = IdentityServerConstants.LocalIdentityProvider
                }.CreatePrincipal(),
                Client = new Client
                {
                    EnableLocalLogin = false,
                    IdentityProviderRestrictions = new List<string> 
                    {
                        "some_idp"
                    }
                }
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task Authenticated_User_with_allowed_requested_Idp_must_not_SignIn()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Client = new Client(),
                 AuthenticationContextReferenceClasses = new List<string>{
                    "idp:" + IdentityServerConstants.LocalIdentityProvider
                },
                Subject = new IdentityServerUser("123")
                {
                    IdentityProvider = IdentityServerConstants.LocalIdentityProvider
                }.CreatePrincipal()
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeFalse();
        }

        [Fact]
        public async Task Authenticated_User_with_different_requested_Idp_must_SignIn()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Client = new Client(),
                AuthenticationContextReferenceClasses = new List<string>{
                    "idp:some_idp"
                },
                Subject = new IdentityServerUser("123")
                {
                    IdentityProvider = IdentityServerConstants.LocalIdentityProvider
                }.CreatePrincipal()
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task Authenticated_User_within_client_user_sso_lifetime_should_not_signin()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Client = new Client() {
                    UserSsoLifetime = 3600 // 1h
                },
                Subject = new IdentityServerUser("123")
                {
                    IdentityProvider = "local",
                    AuthenticationTime = _clock.UtcNow.UtcDateTime.Subtract(TimeSpan.FromSeconds(10))
                }.CreatePrincipal()
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeFalse();
        }

        [Fact]
        public async Task Authenticated_User_beyond_client_user_sso_lifetime_should_signin()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Client = new Client()
                {
                    UserSsoLifetime = 3600 // 1h
                },
                Subject = new IdentityServerUser("123")
                {
                    IdentityProvider = "local",
                    AuthenticationTime = _clock.UtcNow.UtcDateTime.Subtract(TimeSpan.FromSeconds(3700))
                }.CreatePrincipal()
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task locally_authenticated_user_but_client_does_not_allow_local_should_sign_in()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Client = new Client()
                {
                    EnableLocalLogin = false
                },
                Subject = new IdentityServerUser("123")
                {
                    IdentityProvider = IdentityServerConstants.LocalIdentityProvider
                }.CreatePrincipal()
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task prompt_login_should_sign_in()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Subject = new IdentityServerUser("123").CreatePrincipal(),
                PromptModes = new[] { OidcConstants.PromptModes.Login },
                Raw = new NameValueCollection()
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task prompt_select_account_should_sign_in()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Subject = new IdentityServerUser("123").CreatePrincipal(),
                PromptModes = new[] { OidcConstants.PromptModes.SelectAccount },
                Raw = new NameValueCollection()
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task prompt_for_signin_should_remove_prompt_from_raw_url()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Subject = new IdentityServerUser("123").CreatePrincipal(),
                PromptModes = new[] { OidcConstants.PromptModes.Login },
                Raw = new NameValueCollection
                {
                    { OidcConstants.AuthorizeRequest.Prompt, OidcConstants.PromptModes.Login }
                }
            };

            var result = await _subject.ProcessLoginAsync(request);

            request.Raw.AllKeys.Should().NotContain(OidcConstants.AuthorizeRequest.Prompt);
        }
    }
}
