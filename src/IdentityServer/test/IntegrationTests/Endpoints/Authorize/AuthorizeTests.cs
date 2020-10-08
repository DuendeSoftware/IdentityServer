// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityModel;
using IdentityServer.IntegrationTests.Common;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Default;
using IdentityServer4.Test;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityServer.IntegrationTests.Endpoints.Authorize
{
    public class AuthorizeTests
    {
        private const string Category = "Authorize endpoint";

        private IdentityServerPipeline _mockPipeline = new IdentityServerPipeline();

        private Client _client1;

        public AuthorizeTests()
        {
            _mockPipeline.Clients.AddRange(new Client[] {
                _client1 = new Client
                {
                    ClientId = "client1",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    RequireConsent = false,
                    
                    AllowedScopes = new List<string> { "openid", "profile" },
                    RedirectUris = new List<string> { "https://client1/callback" },
                    AllowAccessTokensViaBrowser = true
                },
                new Client
                {
                    ClientId = "client2",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    RequireConsent = true,
                    
                    AllowedScopes = new List<string> { "openid", "profile", "api1", "api2" },
                    RedirectUris = new List<string> { "https://client2/callback" },
                    AllowAccessTokensViaBrowser = true
                },
                new Client
                {
                    ClientId = "client3",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    RequireConsent = false,
                    
                    AllowedScopes = new List<string> { "openid", "profile", "api1", "api2" },
                    RedirectUris = new List<string> { "https://client3/callback" },
                    AllowAccessTokensViaBrowser = true,
                    EnableLocalLogin = false,
                    IdentityProviderRestrictions = new List<string> { "google" }
                },
                new Client
                {
                    ClientId = "client4",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequireClientSecret = false,
                    RequireConsent = false,
                    RequirePkce = false,
                    AllowedScopes = new List<string> { "openid", "profile", "api1", "api2" },
                    RedirectUris = new List<string> { "https://client4/callback" },
                },

            });

            _mockPipeline.Users.Add(new TestUser
            {
                SubjectId = "bob",
                Username = "bob",
                Claims = new Claim[]
                {
                    new Claim("name", "Bob Loblaw"),
                    new Claim("email", "bob@loblaw.com"),
                    new Claim("role", "Attorney")
                }
            });

            _mockPipeline.IdentityScopes.AddRange(new IdentityResource[] {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email()
            });
            _mockPipeline.ApiResources.AddRange(new ApiResource[] {
                new ApiResource
                {
                    Name = "api",
                    Scopes = { "api1", "api2" }
                }
            });
            _mockPipeline.ApiScopes.AddRange(new ApiScope[] {
                new ApiScope
                {
                    Name = "api1"
                },
                new ApiScope
                {
                    Name = "api2"
                }
            });

            _mockPipeline.Initialize();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task get_request_should_not_return_404()
        {
            var response = await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.AuthorizeEndpoint);

            response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task post_request_without_form_should_return_415()
        {
            var response = await _mockPipeline.BrowserClient.PostAsync(IdentityServerPipeline.AuthorizeEndpoint, new StringContent("foo"));

            response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task post_request_should_return_200()
        {
            var response = await _mockPipeline.BrowserClient.PostAsync(IdentityServerPipeline.AuthorizeEndpoint,
                new FormUrlEncodedContent(
                    new Dictionary<string, string> { }));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task get_request_should_not_return_500()
        {
            var response = await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.AuthorizeEndpoint);

            ((int)response.StatusCode).Should().BeLessThan(500);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task anonymous_user_should_be_redirected_to_login_page()
        {
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.LoginWasCalled.Should().BeTrue();
        }

        [Theory]
        [InlineData((Type)null)]
        [InlineData(typeof(QueryStringAuthorizationParametersMessageStore))]
        [InlineData(typeof(DistributedCacheAuthorizationParametersMessageStore))]
        [Trait("Category", Category)]
        public async Task signin_request_should_have_authorization_params(Type storeType)
        {
            if (storeType != null)
            {
                _mockPipeline.OnPostConfigureServices += services =>
                {
                    services.AddTransient(typeof(IAuthorizationParametersMessageStore), storeType);
                };
                _mockPipeline.Initialize();
            }

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce",
                loginHint: "login_hint_value",
                acrValues: "acr_1 acr_2 tenant:tenant_value idp:idp_value",
                extra: new
                {
                    display = "popup", // must use a valid value from the spec for display
                    ui_locales = "ui_locale_value",
                    custom_foo = "foo_value"
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url + "&foo=bar");

            _mockPipeline.LoginRequest.Should().NotBeNull();
            _mockPipeline.LoginRequest.Client.ClientId.Should().Be("client1");
            _mockPipeline.LoginRequest.DisplayMode.Should().Be("popup");
            _mockPipeline.LoginRequest.UiLocales.Should().Be("ui_locale_value");
            _mockPipeline.LoginRequest.IdP.Should().Be("idp_value");
            _mockPipeline.LoginRequest.Tenant.Should().Be("tenant_value");
            _mockPipeline.LoginRequest.LoginHint.Should().Be("login_hint_value");
            _mockPipeline.LoginRequest.AcrValues.Should().BeEquivalentTo(new string[] { "acr_2", "acr_1" });
            _mockPipeline.LoginRequest.Parameters.AllKeys.Should().Contain("foo");
            _mockPipeline.LoginRequest.Parameters["foo"].Should().Be("bar");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task signin_response_should_allow_successful_authorization_response()
        {
            _mockPipeline.Subject = new IdentityServerUser("bob").CreatePrincipal();
            _mockPipeline.BrowserClient.StopRedirectingAfter = 2;

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location.ToString().Should().StartWith("https://client1/callback");

            var authorization = new IdentityModel.Client.AuthorizeResponse(response.Headers.Location.ToString());
            authorization.IsError.Should().BeFalse();
            authorization.IdentityToken.Should().NotBeNull();
            authorization.State.Should().Be("123_state");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authenticated_user_with_valid_request_should_receive_authorization_response()
        {
            await _mockPipeline.LoginAsync("bob");

            _mockPipeline.BrowserClient.AllowAutoRedirect = false;

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location.ToString().Should().StartWith("https://client1/callback");

            var authorization = new IdentityModel.Client.AuthorizeResponse(response.Headers.Location.ToString());
            authorization.IsError.Should().BeFalse();
            authorization.IdentityToken.Should().NotBeNull();
            authorization.State.Should().Be("123_state");
        }

        [Theory]
        [InlineData((Type)null)]
        [InlineData(typeof(QueryStringAuthorizationParametersMessageStore))]
        [InlineData(typeof(DistributedCacheAuthorizationParametersMessageStore))]
        [Trait("Category", Category)]
        public async Task login_response_and_consent_response_should_receive_authorization_response(Type storeType)
        {
            if (storeType != null)
            {
                _mockPipeline.OnPostConfigureServices += services =>
                {
                    services.AddTransient(typeof(IAuthorizationParametersMessageStore), storeType);
                };
                _mockPipeline.Initialize();
            }

            _mockPipeline.Subject = new IdentityServerUser("bob").CreatePrincipal();

            _mockPipeline.ConsentResponse = new ConsentResponse()
            {
                ScopesValuesConsented = new string[] { "openid", "api1", "profile" }
            };

            _mockPipeline.BrowserClient.StopRedirectingAfter = 4;

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client2",
                responseType: "id_token token",
                scope: "openid profile api1 api2",
                redirectUri: "https://client2/callback",
                state: "123_state",
                nonce: "123_nonce");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location.ToString().Should().StartWith("https://client2/callback");

            var authorization = new IdentityModel.Client.AuthorizeResponse(response.Headers.Location.ToString());
            authorization.IsError.Should().BeFalse();
            authorization.IdentityToken.Should().NotBeNull();
            authorization.State.Should().Be("123_state");
            var scopes = authorization.Scope.Split(' ');
            scopes.Should().BeEquivalentTo(new string[] { "profile", "api1", "openid" });
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task idp_should_be_passed_to_login_page()
        {
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client3",
                responseType: "id_token",
                scope: "openid profile",
                redirectUri: "https://client3/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "idp:google");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.LoginWasCalled.Should().BeTrue();
            _mockPipeline.LoginRequest.IdP.Should().Be("google");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task idp_not_allowed_by_client_should_not_be_passed_to_login_page()
        {
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client3",
                responseType: "id_token",
                scope: "openid profile",
                redirectUri: "https://client3/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "idp:facebook");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.LoginWasCalled.Should().BeTrue();
            _mockPipeline.LoginRequest.IdP.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task user_idp_not_allowed_by_client_should_cause_login_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client3",
                responseType: "id_token",
                scope: "openid profile",
                redirectUri: "https://client3/callback",
                state: "123_state",
                nonce: "123_nonce");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.LoginWasCalled.Should().BeTrue();
            _mockPipeline.LoginRequest.IdP.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task for_invalid_client_error_page_should_not_receive_client_id()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: null,
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://invalid",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.ClientId.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task error_page_should_receive_client_id()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://invalid",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.ClientId.Should().Be("client1");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task invalid_redirect_uri_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://invalid",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidRequest);
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Contain("redirect_uri");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task invalid_redirect_uri_should_not_pass_return_url_to_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://invalid",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.RedirectUri.Should().BeNull();
            _mockPipeline.ErrorMessage.ResponseMode.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task invalid_client_id_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1_invalid",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.UnauthorizedClient);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task invalid_client_id_should_not_pass_return_url_to_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1_invalid",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.RedirectUri.Should().BeNull();
            _mockPipeline.ErrorMessage.ResponseMode.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task missing_redirect_uri_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1_invalid",
                responseType: "id_token",
                scope: "openid",
                //redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.UnauthorizedClient);
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Contain("client");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task missing_redirect_uri_should_not_pass_return_url_to_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1_invalid",
                responseType: "id_token",
                scope: "openid",
                //redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.RedirectUri.Should().BeNull();
            _mockPipeline.ErrorMessage.ResponseMode.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task malformed_redirect_uri_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1_invalid",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "invalid-uri",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.UnauthorizedClient);
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Contain("client");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task disabled_client_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            _client1.Enabled = false;

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.UnauthorizedClient);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task disabled_client_should_not_pass_return_url_to_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            _client1.Enabled = false;

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.RedirectUri.Should().BeNull();
            _mockPipeline.ErrorMessage.ResponseMode.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task invalid_protocol_for_client_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            _client1.ProtocolType = "invalid";

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.UnauthorizedClient);
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Contain("protocol");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task invalid_protocol_for_client_should_not_pass_return_url_to_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            _client1.ProtocolType = "invalid";

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.RedirectUri.Should().BeNull();
            _mockPipeline.ErrorMessage.ResponseMode.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task invalid_response_type_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "invalid",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.UnsupportedResponseType);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task invalid_response_type_should_not_pass_return_url_to_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "invalid",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.RedirectUri.Should().BeNull();
            _mockPipeline.ErrorMessage.ResponseMode.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task invalid_response_mode_for_flow_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                responseMode: "query",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidRequest);
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Contain("response_mode");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task invalid_response_mode_for_flow_should_pass_return_url_to_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                responseMode: "query",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.RedirectUri.Should().StartWith("https://client1/callback");
            _mockPipeline.ErrorMessage.ResponseMode.Should().Be("fragment");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task invalid_response_mode_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                responseMode: "invalid",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.UnsupportedResponseType);
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Contain("response_mode");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task invalid_response_mode_should_pass_return_url_to_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                responseMode: "invalid",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.RedirectUri.Should().StartWith("https://client1/callback");
            _mockPipeline.ErrorMessage.ResponseMode.Should().Be("fragment");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task missing_scope_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                //scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidRequest);
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Contain("scope");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task missing_scope_should_pass_return_url_to_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                //scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.RedirectUri.Should().StartWith("https://client1/callback");
            _mockPipeline.ErrorMessage.ResponseMode.Should().Be("fragment");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task explicit_response_mode_should_be_passed_to_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                responseMode: "form_post",
                //scope: "openid", // this will cause the error
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.RedirectUri.Should().StartWith("https://client1/callback");
            _mockPipeline.ErrorMessage.ResponseMode.Should().Be("form_post");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task scope_too_long_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: new string('x', 500),
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidRequest);
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Contain("scope");
            _mockPipeline.ErrorMessage.RedirectUri.Should().StartWith("https://client1/callback");
            _mockPipeline.ErrorMessage.ResponseMode.Should().Be("fragment");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task missing_openid_scope_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "profile",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidRequest);
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Contain("scope");
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Contain("openid");
            _mockPipeline.ErrorMessage.RedirectUri.Should().StartWith("https://client1/callback");
            _mockPipeline.ErrorMessage.ResponseMode.Should().Be("fragment");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task client_not_allowed_access_to_scope_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid email",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidScope);
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Contain("scope");
            _mockPipeline.ErrorMessage.RedirectUri.Should().StartWith("https://client1/callback");
            _mockPipeline.ErrorMessage.ResponseMode.Should().Be("fragment");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task missing_nonce_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state"
            //nonce: "123_nonce"
            );
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidRequest);
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Contain("nonce");
            _mockPipeline.ErrorMessage.RedirectUri.Should().StartWith("https://client1/callback");
            _mockPipeline.ErrorMessage.ResponseMode.Should().Be("fragment");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task nonce_too_long_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: new string('x', 500));
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidRequest);
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Contain("nonce");
            _mockPipeline.ErrorMessage.RedirectUri.Should().StartWith("https://client1/callback");
            _mockPipeline.ErrorMessage.ResponseMode.Should().Be("fragment");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task locale_too_long_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce",
                extra: new { ui_locales = new string('x', 500) });
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidRequest);
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Contain("ui_locales");
            _mockPipeline.ErrorMessage.RedirectUri.Should().StartWith("https://client1/callback");
            _mockPipeline.ErrorMessage.ResponseMode.Should().Be("fragment");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task invalid_max_age_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce",
                extra: new { max_age = "invalid" });
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidRequest);
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Contain("max_age");
            _mockPipeline.ErrorMessage.RedirectUri.Should().StartWith("https://client1/callback");
            _mockPipeline.ErrorMessage.ResponseMode.Should().Be("fragment");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task negative_max_age_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce",
                extra: new { max_age = "-10" });
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidRequest);
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Contain("max_age");
            _mockPipeline.ErrorMessage.RedirectUri.Should().StartWith("https://client1/callback");
            _mockPipeline.ErrorMessage.ResponseMode.Should().Be("fragment");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task login_hint_too_long_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce",
                loginHint: new string('x', 500));
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidRequest);
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Contain("login_hint");
            _mockPipeline.ErrorMessage.RedirectUri.Should().StartWith("https://client1/callback");
            _mockPipeline.ErrorMessage.ResponseMode.Should().Be("fragment");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task acr_values_too_long_should_show_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: new string('x', 500));
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidRequest);
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Contain("acr_values");
            _mockPipeline.ErrorMessage.RedirectUri.Should().StartWith("https://client1/callback");
            _mockPipeline.ErrorMessage.ResponseMode.Should().Be("fragment");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task overlapping_identity_scopes_and_api_scopes_should_show_error_page()
        {
            _mockPipeline.IdentityScopes.Add(new IdentityResource("foo", "Foo", new string[] { "name" }));
            _mockPipeline.IdentityScopes.Add(new IdentityResource("bar", "Bar", new string[] { "name" }));
            _mockPipeline.ApiScopes.Add(new ApiScope("foo", "Foo"));
            _mockPipeline.ApiScopes.Add(new ApiScope("bar", "Bar"));

            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid foo bar",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");

            Func<Task> a = () => _mockPipeline.BrowserClient.GetAsync(url);
            a.Should().Throw<Exception>();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task ui_locales_should_be_passed_to_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: new string('x', 500),
                extra: new { ui_locales = "fr-FR" });
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.UiLocales.Should().Be("fr-FR");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task display_mode_should_be_passed_to_error_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: new string('x', 500),
                extra: new { display = "popup" });
            await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.DisplayMode.Should().Be("popup");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task unicode_values_in_url_should_be_processed_correctly()
        {
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce");
            url = url.Replace(IdentityServerPipeline.BaseUrl, "https://грант.рф");

            var result = await _mockPipeline.BackChannelClient.GetAsync(url);
            result.Headers.Location.Authority.Should().Be("xn--80af5akm.xn--p1ai");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task code_flow_with_fragment_response_type_should_be_allowed()
        {
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client4",
                responseType: "code",
                responseMode: "fragment",
                scope: "openid",
                redirectUri: "https://client4/callback",
                state: "123_state",
                nonce: "123_nonce");

            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            _mockPipeline.LoginWasCalled.Should().BeTrue();
        }


        [Fact]
        [Trait("Category", Category)]
        public async Task prompt_login_should_show_login_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client3",
                responseType: "id_token",
                scope: "openid profile",
                redirectUri: "https://client3/callback",
                state: "123_state",
                nonce: "123_nonce",
                extra:new { prompt = "login" }
            );
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.LoginWasCalled.Should().BeTrue();
        }
    }
}
