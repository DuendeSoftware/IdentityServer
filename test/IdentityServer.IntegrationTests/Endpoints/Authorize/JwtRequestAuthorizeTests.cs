// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Duende.IdentityServer;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Stores.Default;
using Duende.IdentityServer.Test;
using FluentAssertions;
using IdentityModel;
using IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace IntegrationTests.Endpoints.Authorize
{
    public class JwtRequestAuthorizeTests
    {
        private const string Category = "Authorize endpoint with JWT requests";

        private readonly IdentityServerPipeline _mockPipeline = new IdentityServerPipeline();
        private readonly Client _client;

        private readonly string _symmetricJwk = @"{ 'kty': 'oct', 'use': 'sig', 'kid': '1', 'k': 'nYA-IFt8xTsdBHe9hunvizcp3Dt7f6qGqudq18kZHNtvqEGjJ9Ud-9x3kbQ-LYfLHS3xM2MpFQFg1JzT_0U_F8DI40oby4TvBDGszP664UgA8_5GjB7Flnrlsap1NlitvNpgQX3lpyTvC2zVuQ-UVsXbBDAaSBUSlnw7SE4LM8Ye2WYZrdCCXL8yAX9vIR7vf77yvNTEcBCI6y4JlvZaqMB4YKVSfygs8XqGGCHjLpE5bvI-A4ESbAUX26cVFvCeDg9pR6HK7BmwPMlO96krgtKZcXEJtUELYPys6-rbwAIdmxJxKxpgRpt0FRv_9fm6YPwG7QivYBX-vRwaodL1TA', 'alg': 'HS256'}";
        private readonly RsaSecurityKey _rsaKey;

        public JwtRequestAuthorizeTests()
        {
            IdentityModelEventSource.ShowPII = true;

            _rsaKey = CryptoHelper.CreateRsaSecurityKey();

            _mockPipeline.Clients.AddRange(new Client[]
            {
                _client = new Client
                {
                    ClientName = "Client with keys",
                    ClientId = "client",
                    Enabled = true,
                    RequireRequestObject = true,

                    RedirectUris = { "https://client/callback" },

                    ClientSecrets =
                    {
                        new Secret
                        {
                            // x509 cert as base64 string
                            Type = IdentityServerConstants.SecretTypes.X509CertificateBase64,
                            Value = Convert.ToBase64String(TestCert.Load().Export(X509ContentType.Cert))
                        },
                        new Secret
                        {
                            // symmetric key as JWK
                            Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                            Value = _symmetricJwk
                        },
                        new Secret
                        {
                            // RSA key as JWK
                            Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                            Value = JsonSerializer.Serialize(JsonWebKeyConverter.ConvertFromRSASecurityKey(_rsaKey))
                        },
                        new Secret
                        {
                            // x509 cert as JWK
                            Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                            Value = JsonSerializer.Serialize(JsonWebKeyConverter.ConvertFromX509SecurityKey(new X509SecurityKey(TestCert.Load())))
                        }
                    },

                    AllowedGrantTypes = GrantTypes.Implicit,

                    AllowedScopes = new List<string>
                    {
                        "openid", "profile", "api1", "api2"
                    }
                },
                _client = new Client
                {
                    ClientName = "Client with keys",
                    ClientId = "client2",
                    Enabled = true,
                    RequireRequestObject = true,

                    RedirectUris = { "https://client/callback" },

                    ClientSecrets =
                    {
                        new Secret
                        {
                            // x509 cert as base64 string
                            Type = IdentityServerConstants.SecretTypes.X509CertificateBase64,
                            Value = Convert.ToBase64String(TestCert.Load().Export(X509ContentType.Cert))
                        },
                        new Secret
                        {
                            // symmetric key as JWK
                            Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                            Value = _symmetricJwk
                        },
                        new Secret
                        {
                            // RSA key as JWK
                            Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                            Value = JsonSerializer.Serialize(JsonWebKeyConverter.ConvertFromRSASecurityKey(_rsaKey))
                        },
                        new Secret
                        {
                            // x509 cert as JWK
                            Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                            Value = JsonSerializer.Serialize(JsonWebKeyConverter.ConvertFromX509SecurityKey(new X509SecurityKey(TestCert.Load())))
                        }
                    },

                    AllowedGrantTypes = GrantTypes.Implicit,

                    AllowedScopes = new List<string>
                    {
                        "openid", "profile", "api1", "api2"
                    }
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

        string CreateRequestJwt(string issuer, string audience, SigningCredentials credential, Claim[] claims, bool setJwtTyp = false)
        {
            var handler = new JwtSecurityTokenHandler();
            handler.OutboundClaimTypeMap.Clear();

            var token = handler.CreateJwtSecurityToken(
                issuer: issuer,
                audience: audience,
                signingCredentials: credential,
                subject: Identity.Create("pwd", claims));

            if (setJwtTyp)
            {
                token.Header["typ"] = JwtClaimTypes.JwtTypes.AuthorizationRequest;
            }

            return handler.WriteToken(token);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task missing_request_object_should_fail()
        {
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                scope: "openid profile",
                state: "123state",
                nonce: "123nonce",
                redirectUri: "https://client/callback");

            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorMessage.Error.Should().Be("invalid_request");
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Be("Client must use request object, but no request or request_uri parameter present");
            _mockPipeline.LoginRequest.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authorize_should_accept_valid_JWT_request_object_parameters_using_X509_certificate()
        {
            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("client_id", _client.ClientId),
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
            });

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request = requestJwt
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.LoginRequest.Should().NotBeNull();
            _mockPipeline.LoginRequest.Client.ClientId.Should().Be(_client.ClientId);
            _mockPipeline.LoginRequest.DisplayMode.Should().Be("popup");
            _mockPipeline.LoginRequest.UiLocales.Should().Be("ui_locale_value");
            _mockPipeline.LoginRequest.IdP.Should().Be("idp_value");
            _mockPipeline.LoginRequest.Tenant.Should().Be("tenant_value");
            _mockPipeline.LoginRequest.LoginHint.Should().Be("login_hint_value");
            _mockPipeline.LoginRequest.AcrValues.Should().BeEquivalentTo(new string[] { "acr_2", "acr_1" });

            _mockPipeline.LoginRequest.Parameters.AllKeys.Should().Contain("foo");
            _mockPipeline.LoginRequest.Parameters["foo"].Should().Be("123foo");

            _mockPipeline.LoginRequest.RequestObjectValues.Count().Should().Be(11);
            _mockPipeline.LoginRequest.RequestObjectValues.Single(c => c.Type == "foo" && c.Value == "123foo").Should()
                .NotBeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authorize_should_accept_valid_JWT_request_object_parameters_using_symmetric_jwk()
        {
            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new SigningCredentials(new Microsoft.IdentityModel.Tokens.JsonWebKey(_symmetricJwk), "HS256"),
                claims: new[] {
                    new Claim("client_id", _client.ClientId),
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
            });

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request = requestJwt
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.LoginRequest.Should().NotBeNull();
            _mockPipeline.LoginRequest.Client.ClientId.Should().Be(_client.ClientId);
            _mockPipeline.LoginRequest.DisplayMode.Should().Be("popup");
            _mockPipeline.LoginRequest.UiLocales.Should().Be("ui_locale_value");
            _mockPipeline.LoginRequest.IdP.Should().Be("idp_value");
            _mockPipeline.LoginRequest.Tenant.Should().Be("tenant_value");
            _mockPipeline.LoginRequest.LoginHint.Should().Be("login_hint_value");
            _mockPipeline.LoginRequest.AcrValues.Should().BeEquivalentTo(new string[] { "acr_2", "acr_1" });

            _mockPipeline.LoginRequest.Parameters.AllKeys.Should().Contain("foo");
            _mockPipeline.LoginRequest.Parameters["foo"].Should().Be("123foo");

            _mockPipeline.LoginRequest.RequestObjectValues.Count().Should().Be(11);
            _mockPipeline.LoginRequest.RequestObjectValues.Single(c => c.Type == "foo" && c.Value == "123foo").Should()
                .NotBeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authorize_should_accept_valid_JWT_request_object_parameters_using_rsa_jwk()
        {
            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new SigningCredentials(_rsaKey, "RS256"),
                claims: new[] {
                    new Claim("client_id", _client.ClientId),
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
            });

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request = requestJwt
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.LoginRequest.Should().NotBeNull();
            _mockPipeline.LoginRequest.Client.ClientId.Should().Be(_client.ClientId);
            _mockPipeline.LoginRequest.DisplayMode.Should().Be("popup");
            _mockPipeline.LoginRequest.UiLocales.Should().Be("ui_locale_value");
            _mockPipeline.LoginRequest.IdP.Should().Be("idp_value");
            _mockPipeline.LoginRequest.Tenant.Should().Be("tenant_value");
            _mockPipeline.LoginRequest.LoginHint.Should().Be("login_hint_value");
            _mockPipeline.LoginRequest.AcrValues.Should().BeEquivalentTo(new string[] { "acr_2", "acr_1" });

            _mockPipeline.LoginRequest.Parameters.AllKeys.Should().Contain("foo");
            _mockPipeline.LoginRequest.Parameters["foo"].Should().Be("123foo");

            _mockPipeline.LoginRequest.RequestObjectValues.Count().Should().Be(11);
            _mockPipeline.LoginRequest.RequestObjectValues.Single(c => c.Type == "foo" && c.Value == "123foo").Should()
                .NotBeNull();
        }

        [Theory]
        [InlineData((Type) null)]
        [InlineData(typeof(QueryStringAuthorizationParametersMessageStore))]
        [InlineData(typeof(DistributedCacheAuthorizationParametersMessageStore))]
        [Trait("Category", Category)]
        public async Task authorize_should_accept_valid_JWT_request_object_and_allow_some_parameters_in_query(Type storeType)
        {
            if (storeType != null)
            {
                _mockPipeline.OnPostConfigureServices += services =>
                {
                    services.AddTransient(typeof(IAuthorizationParametersMessageStore), storeType);
                };
                _mockPipeline.Initialize();
            }
            
            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("client_id", _client.ClientId),
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
            });

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                nonce: "nonce",
                state: "state",
                responseType: "id_token",
                extra: new
                {
                    request = requestJwt
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.LoginRequest.Should().NotBeNull();
            _mockPipeline.LoginRequest.Client.ClientId.Should().Be(_client.ClientId);
            _mockPipeline.LoginRequest.DisplayMode.Should().Be("popup");
            _mockPipeline.LoginRequest.UiLocales.Should().Be("ui_locale_value");
            _mockPipeline.LoginRequest.IdP.Should().Be("idp_value");
            _mockPipeline.LoginRequest.Tenant.Should().Be("tenant_value");
            _mockPipeline.LoginRequest.LoginHint.Should().Be("login_hint_value");
            _mockPipeline.LoginRequest.AcrValues.Should().BeEquivalentTo(new string[] { "acr_2", "acr_1" });

            _mockPipeline.LoginRequest.Parameters.AllKeys.Should().Contain("foo");
            _mockPipeline.LoginRequest.Parameters["foo"].Should().Be("123foo");
            _mockPipeline.LoginRequest.Parameters["nonce"].Should().Be("nonce");
            _mockPipeline.LoginRequest.Parameters["state"].Should().Be("state");

            _mockPipeline.LoginRequest.RequestObjectValues.Count().Should().Be(9);
            _mockPipeline.LoginRequest.RequestObjectValues.Single(c => c.Type == "foo" && c.Value == "123foo").Should()
                .NotBeNull();
            _mockPipeline.LoginRequest.RequestObjectValues.SingleOrDefault(c => c.Type == "state").Should().BeNull();
            _mockPipeline.LoginRequest.RequestObjectValues.SingleOrDefault(c => c.Type == "nonce").Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task correct_jwt_typ_should_pass_strict_validation()
        {
            _mockPipeline.Options.StrictJarValidation = true;

            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new SigningCredentials(_rsaKey, "RS256"),
                claims: new[] {
                    new Claim("client_id", _client.ClientId),
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
            }, setJwtTyp: true);

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request = requestJwt
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.LoginRequest.Should().NotBeNull();
            _mockPipeline.LoginRequest.Client.ClientId.Should().Be(_client.ClientId);
            _mockPipeline.LoginRequest.DisplayMode.Should().Be("popup");
            _mockPipeline.LoginRequest.UiLocales.Should().Be("ui_locale_value");
            _mockPipeline.LoginRequest.IdP.Should().Be("idp_value");
            _mockPipeline.LoginRequest.Tenant.Should().Be("tenant_value");
            _mockPipeline.LoginRequest.LoginHint.Should().Be("login_hint_value");
            _mockPipeline.LoginRequest.AcrValues.Should().BeEquivalentTo(new string[] { "acr_2", "acr_1" });

            _mockPipeline.LoginRequest.Parameters.AllKeys.Should().Contain("foo");
            _mockPipeline.LoginRequest.Parameters["foo"].Should().Be("123foo");

            _mockPipeline.LoginRequest.RequestObjectValues.Count().Should().Be(11);
            _mockPipeline.LoginRequest.RequestObjectValues.Single(c => c.Type == "foo" && c.Value == "123foo").Should()
                .NotBeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task missing_jwt_typ_should_error()
        {
            _mockPipeline.Options.StrictJarValidation = true;

            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new SigningCredentials(_rsaKey, "RS256"),
                claims: new[] {
                    new Claim("client_id", _client.ClientId),
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
            });

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request = requestJwt
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorMessage.Error.Should().Be("invalid_request_object");
            _mockPipeline.LoginRequest.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authorize_should_accept_complex_objects_in_request_object()
        {
            var someObj = new { foo = new { bar = "bar" }, baz = "baz" };
            var someObjJson = JsonSerializer.Serialize(someObj);
            var someArr = new[] { "a", "b", "c" };
            var someArrJson = JsonSerializer.Serialize(someArr);


            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("client_id", _client.ClientId),
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
                    new Claim("someObj", someObjJson, Microsoft.IdentityModel.JsonWebTokens.JsonClaimValueTypes.Json),
                    new Claim("someArr", someArrJson, Microsoft.IdentityModel.JsonWebTokens.JsonClaimValueTypes.JsonArray),
            });

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request = requestJwt
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.LoginRequest.Should().NotBeNull();

            _mockPipeline.LoginRequest.Parameters["someObj"].Should().NotBeNull();
            var value = _mockPipeline.LoginRequest.Parameters["someObj"];
            var someObj2 = JsonSerializer.Deserialize(value, someObj.GetType());
            someObj.Should().BeEquivalentTo(someObj2);

            _mockPipeline.LoginRequest.Parameters["someArr"].Should().NotBeNull();
            var arrValue = _mockPipeline.LoginRequest.Parameters.GetValues("someArr");
            arrValue.Length.Should().Be(3);

            _mockPipeline.LoginRequest.RequestObjectValues.Count().Should().Be(15);
            value = _mockPipeline.LoginRequest.RequestObjectValues.Single(c => c.Type == "someObj").Value;
            someObj2 = JsonSerializer.Deserialize(value, someObj.GetType());
            someObj.Should().BeEquivalentTo(someObj2);

            var arrValue2 = _mockPipeline.LoginRequest.RequestObjectValues.Where(c => c.Type == "someArr").ToList();
            arrValue2.Count.Should().Be(3);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authorize_should_reject_jwt_request_without_client_id()
        {
            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
            });

            var url = _mockPipeline.CreateAuthorizeUrl(
                responseType: "id_token",
                extra: new
                {
                    request = requestJwt
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorMessage.Error.Should().Be("invalid_request");
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Be("Invalid client_id");
            _mockPipeline.LoginRequest.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authorize_should_reject_jwt_request_without_client_id_in_jwt()
        {
            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
                });

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request = requestJwt
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorMessage.Error.Should().Be("invalid_request_object");
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Be("Invalid JWT request");
            _mockPipeline.LoginRequest.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authorize_should_reject_jwt_request_if_audience_is_incorrect()
        {
            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: "invalid",
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("client_id", _client.ClientId),
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
            });

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request = requestJwt
                });

            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorMessage.Error.Should().Be("invalid_request_object");
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Be("Invalid JWT request");
            _mockPipeline.LoginRequest.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authorize_should_reject_jwt_request_if_issuer_does_not_match_client_id()
        {
            var requestJwt = CreateRequestJwt(
                issuer: "invalid",
                audience: IdentityServerPipeline.BaseUrl,
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("client_id", _client.ClientId),
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
            });

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request = requestJwt
                });

            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorMessage.Error.Should().Be("invalid_request_object");
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Be("Invalid JWT request");
            _mockPipeline.LoginRequest.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authorize_should_reject_jwt_request_that_includes_request_param()
        {
            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
                    new Claim("request", "request")
            });

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request = requestJwt
                });

            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorMessage.Error.Should().Be("invalid_request_object");
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Be("Invalid JWT request");
            _mockPipeline.LoginRequest.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authorize_should_reject_jwt_request_that_includes_request_uri_param()
        {
            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
                    new Claim("request_uri", "request_uri")
            });

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request = requestJwt
                });

            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorMessage.Error.Should().Be("invalid_request_object");
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Be("Invalid JWT request");
            _mockPipeline.LoginRequest.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authorize_should_reject_jwt_request_if_response_type_does_not_match()
        {
            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("response_type", "id_token token"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo")
            });

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request = requestJwt
                });

            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorMessage.Error.Should().Be("invalid_request");
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Be("Invalid JWT request");
            _mockPipeline.LoginRequest.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authorize_should_reject_jwt_request_if_client_id_does_not_match()
        {
            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("response_type", "id_token"),
                    new Claim("client_id", "client"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo")
            });

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client2",
                responseType: "id_token",
                extra: new
                {
                    request = requestJwt
                });

            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorMessage.Error.Should().Be("invalid_request");
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Be("Invalid JWT request");
            _mockPipeline.LoginRequest.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authorize_should_ignore_request_uri_when_feature_is_disabled()
        {
            _mockPipeline.Options.Endpoints.EnableJwtRequestUri = false;

            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("client_id", _client.ClientId),
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
            });
            _mockPipeline.JwtRequestMessageHandler.OnInvoke = req =>
            {
                req.RequestUri.Should().Be(new Uri("http://client_jwt"));
                return Task.CompletedTask;
            };
            _mockPipeline.JwtRequestMessageHandler.Response.Content = new StringContent(requestJwt);


            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request_uri = "http://client_jwt"
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            _mockPipeline.ErrorWasCalled.Should().BeTrue();

            _mockPipeline.JwtRequestMessageHandler.InvokeWasCalled.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authorize_should_accept_request_uri_with_valid_jwt()
        {
            _mockPipeline.Options.Endpoints.EnableJwtRequestUri = true;

            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("client_id", _client.ClientId),
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
                    new Claim("bar", "bar1"),
                    new Claim("bar", "bar2"),
            });
            _mockPipeline.JwtRequestMessageHandler.OnInvoke = req =>
            {
                req.RequestUri.Should().Be(new Uri("http://client_jwt"));
                return Task.CompletedTask;
            };
            _mockPipeline.JwtRequestMessageHandler.Response.Content = new StringContent(requestJwt);


            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request_uri = "http://client_jwt"
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.LoginRequest.Should().NotBeNull();
            _mockPipeline.LoginRequest.Client.ClientId.Should().Be(_client.ClientId);
            _mockPipeline.LoginRequest.DisplayMode.Should().Be("popup");
            _mockPipeline.LoginRequest.UiLocales.Should().Be("ui_locale_value");
            _mockPipeline.LoginRequest.IdP.Should().Be("idp_value");
            _mockPipeline.LoginRequest.Tenant.Should().Be("tenant_value");
            _mockPipeline.LoginRequest.LoginHint.Should().Be("login_hint_value");
            _mockPipeline.LoginRequest.AcrValues.Should().BeEquivalentTo(new string[] { "acr_2", "acr_1" });
            _mockPipeline.LoginRequest.Parameters.AllKeys.Should().Contain("foo");
            _mockPipeline.LoginRequest.Parameters["foo"].Should().Be("123foo");
            _mockPipeline.LoginRequest.RequestObjectValues.Count().Should().Be(13);

            _mockPipeline.JwtRequestMessageHandler.InvokeWasCalled.Should().BeTrue();
        }

        [Theory]
        [InlineData((Type) null)]
        [InlineData(typeof(QueryStringAuthorizationParametersMessageStore))]
        [InlineData(typeof(DistributedCacheAuthorizationParametersMessageStore))]
        [Trait("Category", Category)]
        public async Task authorize_should_accept_request_uri_and_allow_some_parameters_in_query(Type storeType)
        {
            if (storeType != null)
            {
                _mockPipeline.OnPostConfigureServices += services =>
                {
                    services.AddTransient(typeof(IAuthorizationParametersMessageStore), storeType);
                };
                _mockPipeline.Initialize();
            }

            _mockPipeline.Options.Endpoints.EnableJwtRequestUri = true;

            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("client_id", _client.ClientId),
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
                    new Claim("bar", "bar1"),
                    new Claim("bar", "bar2"),
            });
            _mockPipeline.JwtRequestMessageHandler.OnInvoke = req =>
            {
                req.RequestUri.Should().Be(new Uri("http://client_jwt"));
                return Task.CompletedTask;
            };
            _mockPipeline.JwtRequestMessageHandler.Response.Content = new StringContent(requestJwt);


            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                state: "state",
                nonce: "nonce",
                responseType: "id_token",
                extra: new
                {
                    request_uri = "http://client_jwt"
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.LoginRequest.Should().NotBeNull();
            _mockPipeline.LoginRequest.Client.ClientId.Should().Be(_client.ClientId);
            _mockPipeline.LoginRequest.DisplayMode.Should().Be("popup");
            _mockPipeline.LoginRequest.UiLocales.Should().Be("ui_locale_value");
            _mockPipeline.LoginRequest.IdP.Should().Be("idp_value");
            _mockPipeline.LoginRequest.Tenant.Should().Be("tenant_value");
            _mockPipeline.LoginRequest.LoginHint.Should().Be("login_hint_value");
            _mockPipeline.LoginRequest.AcrValues.Should().BeEquivalentTo(new string[] { "acr_2", "acr_1" });
            _mockPipeline.LoginRequest.Parameters.AllKeys.Should().Contain("foo");
            _mockPipeline.LoginRequest.Parameters["foo"].Should().Be("123foo");
            _mockPipeline.LoginRequest.Parameters["nonce"].Should().Be("nonce");
            _mockPipeline.LoginRequest.Parameters["state"].Should().Be("state");
            _mockPipeline.LoginRequest.RequestObjectValues.Count().Should().Be(11);
            _mockPipeline.LoginRequest.RequestObjectValues.Any(x => x.Type == "state").Should().BeFalse();
            _mockPipeline.LoginRequest.RequestObjectValues.Any(x => x.Type == "nonce").Should().BeFalse();

            _mockPipeline.JwtRequestMessageHandler.InvokeWasCalled.Should().BeTrue();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authorize_should_accept_request_uri_with_valid_jwt_and_strict_validation()
        {
            _mockPipeline.Options.Endpoints.EnableJwtRequestUri = true;
            _mockPipeline.Options.StrictJarValidation = true;

            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("client_id", _client.ClientId),
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
            }, setJwtTyp: true);
            _mockPipeline.JwtRequestMessageHandler.OnInvoke = req =>
            {
                req.RequestUri.Should().Be(new Uri("http://client_jwt"));
                return Task.CompletedTask;
            };
            _mockPipeline.JwtRequestMessageHandler.Response.Content = new StringContent(requestJwt);
            _mockPipeline.JwtRequestMessageHandler.Response.Content.Headers.ContentType = new MediaTypeHeaderValue($"application/{JwtClaimTypes.JwtTypes.AuthorizationRequest}");


            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request_uri = "http://client_jwt"
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.LoginRequest.Should().NotBeNull();
            _mockPipeline.LoginRequest.Client.ClientId.Should().Be(_client.ClientId);
            _mockPipeline.LoginRequest.DisplayMode.Should().Be("popup");
            _mockPipeline.LoginRequest.UiLocales.Should().Be("ui_locale_value");
            _mockPipeline.LoginRequest.IdP.Should().Be("idp_value");
            _mockPipeline.LoginRequest.Tenant.Should().Be("tenant_value");
            _mockPipeline.LoginRequest.LoginHint.Should().Be("login_hint_value");
            _mockPipeline.LoginRequest.AcrValues.Should().BeEquivalentTo(new string[] { "acr_2", "acr_1" });
            _mockPipeline.LoginRequest.Parameters.AllKeys.Should().Contain("foo");
            _mockPipeline.LoginRequest.Parameters["foo"].Should().Be("123foo");

            _mockPipeline.JwtRequestMessageHandler.InvokeWasCalled.Should().BeTrue();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authorize_should_reject_request_uri_with_valid_jwt_and_strict_validation_but_invalid_content_type()
        {
            _mockPipeline.Options.Endpoints.EnableJwtRequestUri = true;
            _mockPipeline.Options.StrictJarValidation = true;

            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("client_id", _client.ClientId),
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
            }, setJwtTyp: true);
            _mockPipeline.JwtRequestMessageHandler.OnInvoke = req =>
            {
                req.RequestUri.Should().Be(new Uri("http://client_jwt"));
                return Task.CompletedTask;
            };
            _mockPipeline.JwtRequestMessageHandler.Response.Content = new StringContent(requestJwt);


            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request_uri = "http://client_jwt"
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorMessage.Error.Should().Be("invalid_request_uri");
            _mockPipeline.LoginRequest.Should().BeNull();
            _mockPipeline.JwtRequestMessageHandler.InvokeWasCalled.Should().BeTrue();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task request_uri_response_returns_500_should_fail()
        {
            _mockPipeline.Options.Endpoints.EnableJwtRequestUri = true;

            _mockPipeline.JwtRequestMessageHandler.Response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request_uri = "http://client_jwt"
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.LoginRequest.Should().BeNull();

            _mockPipeline.JwtRequestMessageHandler.InvokeWasCalled.Should().BeTrue();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task request_uri_response_returns_404_should_fail()
        {
            _mockPipeline.Options.Endpoints.EnableJwtRequestUri = true;

            _mockPipeline.JwtRequestMessageHandler.Response = new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request_uri = "http://client_jwt"
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.LoginRequest.Should().BeNull();

            _mockPipeline.JwtRequestMessageHandler.InvokeWasCalled.Should().BeTrue();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task request_uri_length_too_long_should_fail()
        {
            _mockPipeline.Options.Endpoints.EnableJwtRequestUri = true;

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request_uri = "http://" + new string('x', 512)
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.LoginRequest.Should().BeNull();

            _mockPipeline.JwtRequestMessageHandler.InvokeWasCalled.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task both_request_and_request_uri_params_should_fail()
        {
            _mockPipeline.Options.Endpoints.EnableJwtRequestUri = true;

            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("client_id", _client.ClientId),
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("state", "123state"),
                    new Claim("nonce", "123nonce"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                    new Claim("login_hint", "login_hint_value"),
                    new Claim("display", "popup"),
                    new Claim("ui_locales", "ui_locale_value"),
                    new Claim("foo", "123foo"),
            });
            _mockPipeline.JwtRequestMessageHandler.Response.Content = new StringContent(requestJwt);


            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                extra: new
                {
                    request = requestJwt,
                    request_uri = "http://client_jwt"
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.LoginRequest.Should().BeNull();

            _mockPipeline.JwtRequestMessageHandler.InvokeWasCalled.Should().BeFalse();
        }

        [Theory]
        [InlineData((Type) null)]
        [InlineData(typeof(QueryStringAuthorizationParametersMessageStore))]
        [InlineData(typeof(DistributedCacheAuthorizationParametersMessageStore))]
        [Trait("Category", Category)]
        public async Task prompt_login_should_allow_user_to_login_and_complete_authorization(Type storeType)
        {
            if (storeType != null)
            {
                _mockPipeline.OnPostConfigureServices += services =>
                {
                    services.AddTransient(typeof(IAuthorizationParametersMessageStore), storeType);
                };
                _mockPipeline.Initialize();
            }
            
            await _mockPipeline.LoginAsync("bob");

            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("client_id", _client.ClientId),
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("redirect_uri", "https://client/callback"),
                    new Claim("prompt", "login"),
            });

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                state: "state123",
                nonce: "nonce123",
                extra: new
                {
                    request = requestJwt
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            // this simulates the login page returning to the returnUrl which is the authorize callback page
            _mockPipeline.BrowserClient.AllowAutoRedirect = false;
            response = await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.BaseUrl + _mockPipeline.LoginReturnUrl);
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location.ToString().Should().StartWith("https://client/callback");
            response.Headers.Location.ToString().Should().Contain("id_token=");
            response.Headers.Location.ToString().Should().Contain("state=state123");
        }

        [Theory]
        [InlineData((Type) null)]
        [InlineData(typeof(QueryStringAuthorizationParametersMessageStore))]
        [InlineData(typeof(DistributedCacheAuthorizationParametersMessageStore))]
        [Trait("Category", Category)]
        public async Task prompt_login_should_allow_user_to_consent_and_complete_authorization(Type storeType)
        {
            if (storeType != null)
            {
                _mockPipeline.OnPostConfigureServices += services =>
                {
                    services.AddTransient(typeof(IAuthorizationParametersMessageStore), storeType);
                };
                _mockPipeline.Initialize();
            }

            _client.RequireConsent = true;
            
            await _mockPipeline.LoginAsync("bob");

            var requestJwt = CreateRequestJwt(
                issuer: _client.ClientId,
                audience: IdentityServerPipeline.BaseUrl,
                credential: new X509SigningCredentials(TestCert.Load()),
                claims: new[] {
                    new Claim("client_id", _client.ClientId),
                    new Claim("response_type", "id_token"),
                    new Claim("scope", "openid profile"),
                    new Claim("redirect_uri", "https://client/callback"),
            });

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: _client.ClientId,
                responseType: "id_token",
                state: "state123",
                nonce: "nonce123",
                extra: new
                {
                    request = requestJwt
                });

            _mockPipeline.ConsentResponse = new ConsentResponse()
            {
                ScopesValuesConsented = new string[] { "openid", "profile" }
            };
            _mockPipeline.BrowserClient.StopRedirectingAfter = 2;

            var response = await _mockPipeline.BrowserClient.GetAsync(url);


            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location.ToString().Should().StartWith("https://client/callback");
            response.Headers.Location.ToString().Should().Contain("id_token=");
            response.Headers.Location.ToString().Should().Contain("state=state123");
        }
    }
}