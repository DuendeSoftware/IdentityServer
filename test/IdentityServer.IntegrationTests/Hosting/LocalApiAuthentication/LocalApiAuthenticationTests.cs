// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Duende.IdentityServer.Hosting.LocalApiAuthentication;
using Duende.IdentityServer.Models;
using FluentAssertions;
using IdentityModel;
using IdentityModel.Client;
using IntegrationTests.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace IntegrationTests.Hosting.LocalApiAuthentication;

public class LocalApiAuthenticationTests
{
    private const string Category = "Local API Integration";

    private IdentityServerPipeline _pipeline = new IdentityServerPipeline();

    private static string _jwk;
    private Client _client;

    public LocalApiTokenMode Mode { get; set; }
    
    public bool ApiWasCalled { get; set; }
    public ClaimsPrincipal ApiPrincipal { get; set; }

    static LocalApiAuthenticationTests()
    {
        var rsaKey = new RsaSecurityKey(RSA.Create(2048));
        var jsonWebKey = JsonWebKeyConverter.ConvertFromRSASecurityKey(rsaKey);
        jsonWebKey.Alg = "PS256";
        _jwk = JsonSerializer.Serialize(jsonWebKey);
    }

    public LocalApiAuthenticationTests()
    {
        _pipeline.Clients.AddRange(new Client[] {
            _client = new Client
            {
                ClientId = "client",
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret("secret".Sha256()) },
                AllowedScopes = new List<string> { "api1", "api2" },
            }
        });

        _pipeline.ApiResources.AddRange(new ApiResource[] {
            new ApiResource
            {
                Name = "api",
                Scopes = { "api1", "api2" }
            }
        });
        _pipeline.ApiScopes.AddRange(new[] {
            new ApiScope
            {
                Name = "api1"
            },
            new ApiScope
            {
                Name = "api2"
            }
        });

        _pipeline.OnPostConfigureServices += services => 
        {
            services.AddAuthentication()
                .AddLocalApi("local", options => 
                {
                    options.TokenMode = Mode;
                });
        };

        _pipeline.OnPreConfigureServices += services =>
        {
            services.AddRouting();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("token", policy =>
                {
                    policy.AddAuthenticationSchemes("local");
                    policy.RequireAuthenticatedUser();
                });
            });
        };

        _pipeline.OnPreConfigure += app => 
        {
            app.UseRouting();
        };

        _pipeline.OnPostConfigure += app => 
        {
            app.UseAuthorization();

            app.UseEndpoints(eps => 
            {
                eps.MapGet("/api", ctx =>
                { 
                    ApiWasCalled = true;
                    ApiPrincipal = ctx.User;
                    return Task.CompletedTask;
                }).RequireAuthorization("token");
            });
        };

        Init();
    }
    void Init(LocalApiTokenMode mode = LocalApiTokenMode.DPoPAndBearer)
    {
        Mode = mode;
        _pipeline.Initialize();
    }

    async Task<string> GetAccessTokenAsync(bool dpop = false)
    {
        var req = new ClientCredentialsTokenRequest
        {
            Address = "https://server/connect/token",
            ClientId = "client",
            ClientSecret = "secret",
            Scope = "api1",
        };

        if (dpop)
        {
            req.DPoPProofToken = CreateProofToken("POST", "https://server/connect/token");
        }

        var result = await _pipeline.BackChannelClient.RequestClientCredentialsTokenAsync(req);
        result.IsError.Should().BeFalse();

        if (dpop) result.TokenType.Should().Be("DPoP");
        else result.TokenType.Should().Be("Bearer");

        return result.AccessToken;
    }
    string CreateProofToken(string method, string url, string accessToken = null, string nonce = null)
    {
        var jsonWebKey = new Microsoft.IdentityModel.Tokens.JsonWebKey(_jwk);

        // jwk: representing the public key chosen by the client, in JSON Web Key (JWK) [RFC7517] format,
        // as defined in Section 4.1.3 of [RFC7515]. MUST NOT contain a private key.
        object jwk;
        if (string.Equals(jsonWebKey.Kty, JsonWebAlgorithmsKeyTypes.EllipticCurve))
        {
            jwk = new
            {
                kty = jsonWebKey.Kty,
                x = jsonWebKey.X,
                y = jsonWebKey.Y,
                crv = jsonWebKey.Crv
            };
        }
        else if (string.Equals(jsonWebKey.Kty, JsonWebAlgorithmsKeyTypes.RSA))
        {
            jwk = new Dictionary<string, object>
            {
                { "kty", jsonWebKey.Kty },
                { "e", jsonWebKey.E },
                { "n", jsonWebKey.N }
            };
        }
        else
        {
            throw new InvalidOperationException("invalid key type.");
        }

        var header = new Dictionary<string, object>()
        {
            //{ "alg", "RS265" }, // JsonWebTokenHandler requires adding this itself
            { "typ", JwtClaimTypes.JwtTypes.DPoPProofToken },
            { JwtClaimTypes.JsonWebKey, jwk },
        };

        var payload = new Dictionary<string, object>
        {
            { JwtClaimTypes.JwtId, CryptoRandom.CreateUniqueId() },
            { JwtClaimTypes.DPoPHttpMethod, method },
            { JwtClaimTypes.DPoPHttpUrl, url },
            { JwtClaimTypes.IssuedAt, DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
        };

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            // ath: hash of the access token. The value MUST be the result of a base64url encoding 
            // the SHA-256 hash of the ASCII encoding of the associated access token's value.
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(accessToken));
            var ath = Base64Url.Encode(hash);

            payload.Add(JwtClaimTypes.DPoPAccessTokenHash, ath);
        }

        if (!string.IsNullOrEmpty(nonce))
        {
            payload.Add(JwtClaimTypes.Nonce, nonce);
        }

        var handler = new JsonWebTokenHandler() { SetDefaultTimesOnTokenCreation = false };
        var key = new SigningCredentials(jsonWebKey, jsonWebKey.Alg);
            var proofToken = handler.CreateToken(JsonSerializer.Serialize(payload), key, header);
            return proofToken;
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task bearer_jwt_token_should_validate()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "https://server/api");
        var at = await GetAccessTokenAsync();
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", at);

        var response = await _pipeline.BackChannelClient.SendAsync(req);

        response.IsSuccessStatusCode.Should().BeTrue();
        ApiWasCalled.Should().BeTrue();
        ApiPrincipal.Identity.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task bearer_ref_token_should_validate()
    {
        _client.AccessTokenType = AccessTokenType.Reference;

        var req = new HttpRequestMessage(HttpMethod.Get, "https://server/api");
        var at = await GetAccessTokenAsync();
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", at);

        var response = await _pipeline.BackChannelClient.SendAsync(req);

        response.IsSuccessStatusCode.Should().BeTrue();
        ApiWasCalled.Should().BeTrue();
        ApiPrincipal.Identity.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task dpop_token_should_validate()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "https://server/api");
        var at = await GetAccessTokenAsync(true);
        req.Headers.Authorization = new AuthenticationHeaderValue("DPoP", at);
        req.Headers.Add("DPoP", CreateProofToken("GET", "https://server/api", at));

        var response = await _pipeline.BackChannelClient.SendAsync(req);

        response.IsSuccessStatusCode.Should().BeTrue();
        ApiWasCalled.Should().BeTrue();
        ApiPrincipal.Identity.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task dpop_nonce_required_should_require_nonce()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "https://server/api");
        var at = await GetAccessTokenAsync(true);
        req.Headers.Authorization = new AuthenticationHeaderValue("DPoP", at);
        req.Headers.Add("DPoP", CreateProofToken("GET", "https://server/api", at));

        _client.DPoPValidationMode = DPoPTokenExpirationValidationMode.Nonce;
        var response = await _pipeline.BackChannelClient.SendAsync(req);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.Headers.Contains("DPoP-Nonce").Should().BeTrue();
    }
    [Fact]
    [Trait("Category", Category)]
    public async Task dpop_nonce_should_validate()
    {
        var at = await GetAccessTokenAsync(true);
        
        var req = new HttpRequestMessage(HttpMethod.Get, "https://server/api");
        req.Headers.Authorization = new AuthenticationHeaderValue("DPoP", at);
        req.Headers.Add("DPoP", CreateProofToken("GET", "https://server/api", at));

        _client.DPoPValidationMode = DPoPTokenExpirationValidationMode.Nonce;
        var response = await _pipeline.BackChannelClient.SendAsync(req);
        var nonce = response.Headers.GetValues("DPoP-Nonce").FirstOrDefault();

        var req2 = new HttpRequestMessage(HttpMethod.Get, "https://server/api");
        req2.Headers.Authorization = new AuthenticationHeaderValue("DPoP", at);
        req2.Headers.Add("DPoP", CreateProofToken("GET", "https://server/api", at, nonce));

        var response2 = await _pipeline.BackChannelClient.SendAsync(req2);
        response2.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task bearer_only_dpop_token_should_fail()
    {
        Init(LocalApiTokenMode.BearerOnly);
        var req = new HttpRequestMessage(HttpMethod.Get, "https://server/api");
        var at = await GetAccessTokenAsync(true);
        req.Headers.Authorization = new AuthenticationHeaderValue("DPoP", at);
        req.Headers.Add("DPoP", CreateProofToken("GET", "https://server/api", at));

        var response = await _pipeline.BackChannelClient.SendAsync(req);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.Headers.WwwAuthenticate.Select(x => x.Scheme).Should().BeEquivalentTo(new[] { "Bearer" });

    }
    
    [Fact]
    [Trait("Category", Category)]
    public async Task dpop_only_bearer_should_fail()
    {
        Init(LocalApiTokenMode.DPoPOnly);
        var req = new HttpRequestMessage(HttpMethod.Get, "https://server/api");
        var at = await GetAccessTokenAsync();
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", at);

        var response = await _pipeline.BackChannelClient.SendAsync(req);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.Headers.WwwAuthenticate.Select(x => x.Scheme).Should().BeEquivalentTo(new[] { "DPoP" });
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task missing_authz_should_fail()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "https://server/api");

        var response = await _pipeline.BackChannelClient.SendAsync(req);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.Headers.WwwAuthenticate.Select(x => x.Scheme).Should().BeEquivalentTo(new[] { "Bearer", "DPoP" });
    }
    [Fact]
    [Trait("Category", Category)]
    public async Task missing_token_should_fail()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "https://server/api");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

        var response = await _pipeline.BackChannelClient.SendAsync(req);

        response.IsSuccessStatusCode.Should().BeFalse();
    }
    [Fact]
    [Trait("Category", Category)]
    public async Task malformed_token_should_fail()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "https://server/api");
        var at = await GetAccessTokenAsync();
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", at.Substring(at.Length / 2));

        var response = await _pipeline.BackChannelClient.SendAsync(req);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task dpop_token_for_disabled_client_should_fail()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "https://server/api");
        var at = await GetAccessTokenAsync(true);
        req.Headers.Authorization = new AuthenticationHeaderValue("DPoP", at);
        req.Headers.Add("DPoP", CreateProofToken("GET", "https://server/api", at));

        _client.Enabled = false;

        var response = await _pipeline.BackChannelClient.SendAsync(req);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task dpop_validation_failure_should_fail()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "https://server/api");
        var at = await GetAccessTokenAsync(true);
        req.Headers.Authorization = new AuthenticationHeaderValue("DPoP", at);

        var response = await _pipeline.BackChannelClient.SendAsync(req);

        response.IsSuccessStatusCode.Should().BeFalse();
    }
    [Fact]
    [Trait("Category", Category)]
    public async Task dpop_token_using_bearer_scheme_should_fail()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "https://server/api");
        var at = await GetAccessTokenAsync(true);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", at);

        var response = await _pipeline.BackChannelClient.SendAsync(req);

        response.IsSuccessStatusCode.Should().BeFalse();
    }
}
