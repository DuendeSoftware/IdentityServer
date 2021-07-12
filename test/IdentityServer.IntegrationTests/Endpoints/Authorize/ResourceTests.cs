// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Test;
using FluentAssertions;
using IdentityModel.Client;
using IntegrationTests.Common;
using Xunit;

namespace IntegrationTests.Endpoints.Authorize
{
    public class ResourceTests
    {
        private const string Category = "Authorize endpoint";

        private IdentityServerPipeline _mockPipeline = new IdentityServerPipeline();

        private Client _client1;

        public ResourceTests()
        {
            _mockPipeline.Clients.AddRange(new Client[] {
                _client1 = new Client
                {
                    ClientId = "client1",
                    ClientSecrets = { new Secret("secret".Sha256()) },
                    AllowedGrantTypes = GrantTypes.Code,
                    RequireConsent = false,
                    RequirePkce = false,
                    AllowOfflineAccess = true,

                    AllowedScopes = new List<string> { "openid", "profile", "scope1", "scope2", "scope3", "scope4" },
                    RedirectUris = new List<string> { "https://client1/callback" },
                },
                new Client
                {
                    ClientId = "client2",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    RequireConsent = false,
                    AllowAccessTokensViaBrowser = true,

                    AllowedScopes = new List<string> { "openid", "profile", "scope1", "scope2", "scope3", "scope4" },
                    RedirectUris = new List<string> { "https://client2/callback" },
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
                    Name = "urn:resource1",
                    Scopes = { "scope1", "scope2" }
                },
                new ApiResource
                {
                    Name = "urn:resource2",
                    Scopes = { "scope2" }
                },
                new ApiResource
                {
                    RequireResourceIndicator = true,
                    Name = "urn:resource3",
                    Scopes = { "scope3" }
                },
            });
            _mockPipeline.ApiScopes.AddRange(new ApiScope[] {
                new ApiScope
                {
                    Name = "scope1"
                },
                new ApiScope
                {
                    Name = "scope2"
                },
                new ApiScope
                {
                    Name = "scope3"
                },
                new ApiScope
                {
                    Name = "scope4"
                },
            });

            _mockPipeline.Initialize();
        }



        //////////////////////////////////////////////////////////////////
        //// helpers
        //////////////////////////////////////////////////////////////////
        private IEnumerable<Claim> ParseAccessTokenClaims(TokenResponse tokenResponse)
        {
            tokenResponse.IsError.Should().BeFalse();

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenResponse.AccessToken);
            return token.Claims;
        }
        private string GetCode(HttpResponseMessage response)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            var url = response.Headers.Location.ToString();
            var idx = url.IndexOf("code=");
            idx.Should().BeGreaterThan(-1);
            var code = url.Substring(idx + 5);
            idx = code.IndexOf("&");
            if (idx >= 0)
            {
                code = code.Substring(0, idx);
            }
            return code;
        }
        //////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////

        [Fact]
        [Trait("Category", Category)]
        public async Task no_resource_indicator_on_code_exchange_should_succeed()
        {
            await _mockPipeline.LoginAsync("bob");

            _mockPipeline.BrowserClient.AllowAutoRedirect = false;

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "code",
                scope: "openid profile scope1 scope2 scope3 scope4 offline_access",
                redirectUri: "https://client1/callback");

            url += "&resource=urn:resource1";
            url += "&resource=urn:resource3";

            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var code = GetCode(response);

            var tokenResponse = await _mockPipeline.BackChannelClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
            {
                Address = IdentityServerPipeline.TokenEndpoint,
                ClientId = "client1",
                ClientSecret = "secret",
                Code = code,
                RedirectUri = "https://client1/callback"
            });

            {
                var claims = ParseAccessTokenClaims(tokenResponse);
                claims.Where(x => x.Type == "aud").Select(x => x.Value).Should().BeEquivalentTo(new[] { "urn:resource1", "urn:resource2" });
                claims.Where(x => x.Type == "scope").Select(x => x.Value).Should().BeEquivalentTo(new[] { "openid", "profile", "scope1", "scope2", "scope3", "scope4", "offline_access" });
            }
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task resource_indicator_on_code_exchange_should_succeed()
        {
            await _mockPipeline.LoginAsync("bob");

            _mockPipeline.BrowserClient.AllowAutoRedirect = false;

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "code",
                scope: "openid profile scope1 scope2 scope3 scope4 offline_access",
                redirectUri: "https://client1/callback");

            url += "&resource=urn:resource1";
            url += "&resource=urn:resource3";

            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var code = GetCode(response);

            var tokenResponse = await _mockPipeline.BackChannelClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
            {
                Address = IdentityServerPipeline.TokenEndpoint,
                ClientId = "client1",
                ClientSecret = "secret",
                Code = code,
                RedirectUri = "https://client1/callback",
                Parameters =
                {
                    { "resource", "urn:resource1" }
                }
            });

            {
                var claims = ParseAccessTokenClaims(tokenResponse);
                claims.Where(x => x.Type == "aud").Select(x => x.Value).Should().BeEquivalentTo(new[] { "urn:resource1" });
                claims.Where(x => x.Type == "scope").Select(x => x.Value).Should().BeEquivalentTo(new[] { "scope1", "scope2", "offline_access" });
            }
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task resource_indicator_on_refresh_token_exchange_should_succeed()
        {
            await _mockPipeline.LoginAsync("bob");

            _mockPipeline.BrowserClient.AllowAutoRedirect = false;

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "code",
                scope: "openid profile scope1 scope2 scope3 scope4 offline_access",
                redirectUri: "https://client1/callback");

            url += "&resource=urn:resource1";
            url += "&resource=urn:resource3";

            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var code = GetCode(response);

            var tokenResponse = await _mockPipeline.BackChannelClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
            {
                Address = IdentityServerPipeline.TokenEndpoint,
                ClientId = "client1",
                ClientSecret = "secret",
                Code = code,
                RedirectUri = "https://client1/callback"
            });

            tokenResponse = await _mockPipeline.BackChannelClient.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = IdentityServerPipeline.TokenEndpoint,
                ClientId = "client1",
                ClientSecret = "secret",
                RefreshToken = tokenResponse.RefreshToken,
                Parameters =
                {
                    { "resource", "urn:resource1" }
                }
            });

            {
                var claims = ParseAccessTokenClaims(tokenResponse);
                claims.Where(x => x.Type == "aud").Select(x => x.Value).Should().BeEquivalentTo(new[] { "urn:resource1" });
                claims.Where(x => x.Type == "scope").Select(x => x.Value).Should().BeEquivalentTo(new[] { "scope1", "scope2", "offline_access" });
            }
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task no_resource_indicator_on_refresh_token_exchange_should_succeed()
        {
            await _mockPipeline.LoginAsync("bob");

            _mockPipeline.BrowserClient.AllowAutoRedirect = false;

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "code",
                scope: "openid profile scope1 scope2 scope3 scope4 offline_access",
                redirectUri: "https://client1/callback");

            url += "&resource=urn:resource1";
            url += "&resource=urn:resource3";

            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var code = GetCode(response);

            var tokenResponse = await _mockPipeline.BackChannelClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
            {
                Address = IdentityServerPipeline.TokenEndpoint,
                ClientId = "client1",
                ClientSecret = "secret",
                Code = code,
                RedirectUri = "https://client1/callback"
            });

            tokenResponse = await _mockPipeline.BackChannelClient.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = IdentityServerPipeline.TokenEndpoint,
                ClientId = "client1",
                ClientSecret = "secret",
                RefreshToken = tokenResponse.RefreshToken,
            });

            {
                var claims = ParseAccessTokenClaims(tokenResponse);
                claims.Where(x => x.Type == "aud").Select(x => x.Value).Should().BeEquivalentTo(new[] { "urn:resource1", "urn:resource2" });
                claims.Where(x => x.Type == "scope").Select(x => x.Value).Should().BeEquivalentTo(new[] { "openid", "profile", "scope1", "scope2", "scope3", "scope4", "offline_access" });
            }
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task resource_indicator_without_offline_access_should_succeed()
        {
            await _mockPipeline.LoginAsync("bob");

            _mockPipeline.BrowserClient.AllowAutoRedirect = false;

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "code",
                scope: "scope1 scope2",
                redirectUri: "https://client1/callback");

            url += "&resource=urn:resource1";

            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var code = GetCode(response);

            var tokenResponse = await _mockPipeline.BackChannelClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
            {
                Address = IdentityServerPipeline.TokenEndpoint,
                ClientId = "client1",
                ClientSecret = "secret",
                Code = code,
                RedirectUri = "https://client1/callback",
                Parameters =
                {
                    { "resource", "urn:resource1" }
                }
            });

            {
                var claims = ParseAccessTokenClaims(tokenResponse);
                claims.Where(x => x.Type == "aud").Select(x => x.Value).Should().BeEquivalentTo(new[] { "urn:resource1" });
                claims.Where(x => x.Type == "scope").Select(x => x.Value).Should().BeEquivalentTo(new[] { "scope1", "scope2" });
            }
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task implicit_flow_with_resource_indicator_should_fail()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client2",
                responseType: "id_token token",
                scope: "openid profile scope1 scope2 scope3 scope4",
                nonce:"nonce",
                redirectUri: "https://client2/callback");

            url += "&resource=urn:resource1";
            url += "&resource=urn:resource3";

            await _mockPipeline.BrowserClient.GetAsync(url);
            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.Error.Should().Be("invalid_target");
        }
    }
}
