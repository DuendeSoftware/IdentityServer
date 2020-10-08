// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityModel;
using IdentityModel.Client;
using IdentityServer.IntegrationTests.Clients.Setup;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace IdentityServer.IntegrationTests.Clients
{
    public class ResourceOwnerClient
    {
        private const string TokenEndpoint = "https://server/connect/token";

        private readonly HttpClient _client;

        public ResourceOwnerClient()
        {
            var builder = new WebHostBuilder()
                .UseStartup<Startup>();
            var server = new TestServer(builder);

            _client = server.CreateClient();
        }

        [Fact]
        public async Task Valid_user_should_succeed_with_expected_response_payload()
        {
            var response = await _client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "roclient",
                ClientSecret = "secret",

                Scope = "api1",
                UserName = "bob",
                Password = "bob"
            });

            response.IsError.Should().Be(false);
            response.ExpiresIn.Should().Be(3600);
            response.TokenType.Should().Be("Bearer");
            response.IdentityToken.Should().BeNull();
            response.RefreshToken.Should().BeNull();

            var payload = GetPayload(response);

            payload.Count().Should().Be(12);
            payload.Should().Contain("iss", "https://idsvr4");
            payload.Should().Contain("client_id", "roclient");
            payload.Should().Contain("sub", "88421113");
            payload.Should().Contain("idp", "local");
            payload.Keys.Should().Contain("jti");
            payload.Keys.Should().Contain("iat");
            
            payload["aud"].Should().Be("api");

            var scopes = ((JArray)payload["scope"]).Select(x => x.ToString());
            scopes.Count().Should().Be(1);
            scopes.Should().Contain("api1");

            var amr = payload["amr"] as JArray;
            amr.Count().Should().Be(1);
            amr.First().ToString().Should().Be("pwd");
        }

        [Fact]
        public async Task Request_with_no_explicit_scopes_should_return_allowed_scopes()
        {
            var response = await _client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "roclient",
                ClientSecret = "secret",

                UserName = "bob",
                Password = "bob"
            });

            response.IsError.Should().Be(false);
            response.ExpiresIn.Should().Be(3600);
            response.TokenType.Should().Be("Bearer");
            response.IdentityToken.Should().BeNull();
            response.RefreshToken.Should().NotBeNull();

            var payload = GetPayload(response);
            
            payload.Should().Contain("iss", "https://idsvr4");
            payload.Should().Contain("client_id", "roclient");
            payload.Should().Contain("sub", "88421113");
            payload.Should().Contain("idp", "local");

            payload["aud"].Should().Be("api");

            var amr = payload["amr"] as JArray;
            amr.Count().Should().Be(1);
            amr.First().ToString().Should().Be("pwd");

            var scopes = ((JArray)payload["scope"]).Select(x => x.ToString());
            scopes.Count().Should().Be(8);

            // {[  "address",  "api1",  "api2", "api4.with.roles", "email",  "offline_access",  "openid", "role"]}

            scopes.Should().Contain("address");
            scopes.Should().Contain("api1");
            scopes.Should().Contain("api2");
            scopes.Should().Contain("api4.with.roles");
            scopes.Should().Contain("email");
            scopes.Should().Contain("offline_access");
            scopes.Should().Contain("openid");
            scopes.Should().Contain("roles");
        }

        [Fact]
        public async Task Request_containing_identity_scopes_should_return_expected_payload()
        {
            var response = await _client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "roclient",
                ClientSecret = "secret",

                Scope = "openid email api1",
                UserName = "bob",
                Password = "bob"
            });

            response.IsError.Should().Be(false);
            response.ExpiresIn.Should().Be(3600);
            response.TokenType.Should().Be("Bearer");
            response.IdentityToken.Should().BeNull();
            response.RefreshToken.Should().BeNull();

            var payload = GetPayload(response);

            payload.Count().Should().Be(12);
            payload.Should().Contain("iss", "https://idsvr4");
            payload.Should().Contain("client_id", "roclient");
            payload.Should().Contain("sub", "88421113");
            payload.Should().Contain("idp", "local");
            payload.Keys.Should().Contain("jti");
            payload.Keys.Should().Contain("iat");

            payload["aud"].Should().Be("api");

            var amr = payload["amr"] as JArray;
            amr.Count().Should().Be(1);
            amr.First().ToString().Should().Be("pwd");

            var scopes = ((JArray)payload["scope"]).Select(x=>x.ToString());
            scopes.Count().Should().Be(3);
            scopes.Should().Contain("api1");
            scopes.Should().Contain("email");
            scopes.Should().Contain("openid");
        }

        [Fact]
        public async Task Request_for_refresh_token_should_return_expected_payload()
        {
            var response = await _client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "roclient",
                ClientSecret = "secret",

                Scope = "openid email api1 offline_access",
                UserName = "bob",
                Password = "bob"
            });

            response.IsError.Should().Be(false);
            response.ExpiresIn.Should().Be(3600);
            response.TokenType.Should().Be("Bearer");
            response.IdentityToken.Should().BeNull();
            response.RefreshToken.Should().NotBeNullOrWhiteSpace();

            var payload = GetPayload(response);

            payload.Count().Should().Be(12);
            payload.Should().Contain("iss", "https://idsvr4");
            payload.Should().Contain("client_id", "roclient");
            payload.Should().Contain("sub", "88421113");
            payload.Should().Contain("idp", "local");
            payload.Keys.Should().Contain("jti");
            payload.Keys.Should().Contain("iat");

            payload["aud"].Should().Be("api");

            var amr = payload["amr"] as JArray;
            amr.Count().Should().Be(1);
            amr.First().ToString().Should().Be("pwd");

            var scopes = ((JArray)payload["scope"]).Select(x => x.ToString());
            scopes.Count().Should().Be(4);
            scopes.Should().Contain("api1");
            scopes.Should().Contain("email");
            scopes.Should().Contain("offline_access");
            scopes.Should().Contain("openid");
        }

        [Fact]
        public async Task Unknown_user_should_fail()
        {
            var response = await _client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "roclient",
                ClientSecret = "secret",

                Scope = "api1",
                UserName = "unknown",
                Password = "bob"
            });

            response.IsError.Should().Be(true);
            response.ErrorType.Should().Be(ResponseErrorType.Protocol);
            response.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Error.Should().Be("invalid_grant");
        }
        
        [Fact]
        public async Task User_with_empty_password_should_succeed()
        {
            var response = await _client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "roclient",
                ClientSecret = "secret",

                Scope = "api1",
                UserName = "bob_no_password"
            });

            response.IsError.Should().Be(false);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("")]
        public async Task User_with_invalid_password_should_fail(string password)
        {
            var response = await _client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "roclient",
                ClientSecret = "secret",

                Scope = "api1",
                UserName = "bob",
                Password = password
            });

            response.IsError.Should().Be(true);
            response.ErrorType.Should().Be(ResponseErrorType.Protocol);
            response.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Error.Should().Be("invalid_grant");
        }


        private static Dictionary<string, object> GetPayload(IdentityModel.Client.TokenResponse response)
        {
            var token = response.AccessToken.Split('.').Skip(1).Take(1).First();
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                Encoding.UTF8.GetString(Base64Url.Decode(token)));

            return dictionary;
        }
    }
}