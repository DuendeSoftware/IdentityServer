// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityModel;
using IdentityServer.IntegrationTests.Common;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Xunit;

namespace IdentityServer.IntegrationTests.Endpoints.Authorize
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
                            Value = JsonConvert.SerializeObject(JsonWebKeyConverter.ConvertFromRSASecurityKey(_rsaKey))
                        },
                        new Secret
                        {
                            // x509 cert as JWK
                            Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                            Value = JsonConvert.SerializeObject(JsonWebKeyConverter.ConvertFromX509SecurityKey(new X509SecurityKey(TestCert.Load())))
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
                            Value = JsonConvert.SerializeObject(JsonWebKeyConverter.ConvertFromRSASecurityKey(_rsaKey))
                        },
                        new Secret
                        {
                            // x509 cert as JWK
                            Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                            Value = JsonConvert.SerializeObject(JsonWebKeyConverter.ConvertFromX509SecurityKey(new X509SecurityKey(TestCert.Load())))
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

            _mockPipeline.LoginRequest.RequestObjectValues.Count.Should().Be(11);
            _mockPipeline.LoginRequest.RequestObjectValues.Should().ContainKey("foo");
            _mockPipeline.LoginRequest.RequestObjectValues["foo"].Should().Be("123foo");
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

            _mockPipeline.LoginRequest.RequestObjectValues.Count.Should().Be(11);
            _mockPipeline.LoginRequest.RequestObjectValues.Should().ContainKey("foo");
            _mockPipeline.LoginRequest.RequestObjectValues["foo"].Should().Be("123foo");
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

            _mockPipeline.LoginRequest.RequestObjectValues.Count.Should().Be(11);
            _mockPipeline.LoginRequest.RequestObjectValues.Should().ContainKey("foo");
            _mockPipeline.LoginRequest.RequestObjectValues["foo"].Should().Be("123foo");
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

            _mockPipeline.LoginRequest.RequestObjectValues.Count.Should().Be(11);
            _mockPipeline.LoginRequest.RequestObjectValues.Should().ContainKey("foo");
            _mockPipeline.LoginRequest.RequestObjectValues["foo"].Should().Be("123foo");
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
        public async Task mismatch_in_jwt_values_should_error()
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
                scope: "bad",
                state: "bad",
                nonce: "bad",
                redirectUri: "bad",
                acrValues: "bad",
                loginHint: "bad",
                extra: new
                {
                    display = "bad",
                    ui_locales = "bad",
                    foo = "bad",
                    request = requestJwt
                });
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            _mockPipeline.ErrorMessage.Error.Should().Be("invalid_request");
            _mockPipeline.ErrorMessage.ErrorDescription.Should().Be("Parameter mismatch in JWT request");
            _mockPipeline.LoginRequest.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task authorize_should_accept_complex_objects_in_request_object()
        {
            var someObj = new { foo = new { bar = "bar" }, baz = "baz" };
            var someObjJson = JsonConvert.SerializeObject(someObj);
            var someArr = new[] { "a", "b", "c" };
            var someArrJson = JsonConvert.SerializeObject(someArr);


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
            var someObj2 = JsonConvert.DeserializeObject(_mockPipeline.LoginRequest.Parameters["someObj"], someObj.GetType());
            someObj.Should().BeEquivalentTo(someObj2);
            _mockPipeline.LoginRequest.Parameters["someArr"].Should().NotBeNull();
            var someArr2 = JsonConvert.DeserializeObject<string[]>(_mockPipeline.LoginRequest.Parameters["someArr"]);
            someArr2.Should().Contain(new[] { "a", "c", "b" });
            someArr2.Length.Should().Be(3);

            _mockPipeline.LoginRequest.RequestObjectValues.Count.Should().Be(13);
            _mockPipeline.LoginRequest.RequestObjectValues["someObj"].Should().NotBeNull();
            someObj2 = JsonConvert.DeserializeObject(_mockPipeline.LoginRequest.RequestObjectValues["someObj"], someObj.GetType());
            someObj.Should().BeEquivalentTo(someObj2);
            _mockPipeline.LoginRequest.RequestObjectValues["someArr"].Should().NotBeNull();
            someArr2 = JsonConvert.DeserializeObject<string[]>(_mockPipeline.LoginRequest.Parameters["someArr"]);
            someArr2.Should().Contain(new[] { "a", "c", "b" });
            someArr2.Length.Should().Be(3);
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
    }
}