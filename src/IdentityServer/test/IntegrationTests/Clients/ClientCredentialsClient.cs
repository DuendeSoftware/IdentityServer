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
    public class ClientCredentialsClient
    {
        private const string TokenEndpoint = "https://server/connect/token";

        private readonly HttpClient _client;

        public ClientCredentialsClient()
        {
            var builder = new WebHostBuilder()
                .UseStartup<Startup>();
            var server = new TestServer(builder);

            _client = server.CreateClient();
        }

        [Fact]
        public async Task Invalid_endpoint_should_return_404()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint + "invalid",
                ClientId = "client",
                ClientSecret = "secret",
                Scope = "api1"
            });

            response.IsError.Should().Be(true);
            response.ErrorType.Should().Be(ResponseErrorType.Http);
            response.Error.Should().Be("Not Found");
            response.HttpStatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Valid_request_single_audience_should_return_expected_payload()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret",
                Scope = "api1"
            });

            response.IsError.Should().Be(false);
            response.ExpiresIn.Should().Be(3600);
            response.TokenType.Should().Be("Bearer");
            response.IdentityToken.Should().BeNull();
            response.RefreshToken.Should().BeNull();

            var payload = GetPayload(response);

            payload.Count().Should().Be(8);
            payload.Should().Contain("iss", "https://idsvr4");
            payload.Should().Contain("client_id", "client");
            payload.Keys.Should().Contain("jti");
            payload.Keys.Should().Contain("iat");
            
            payload["aud"].Should().Be("api");

            var scopes = payload["scope"] as JArray;
            scopes.First().ToString().Should().Be("api1");
        }

        [Fact]
        public async Task Valid_request_multiple_audiences_should_return_expected_payload()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret",
                Scope = "api1 other_api"
            });

            response.IsError.Should().Be(false);
            response.ExpiresIn.Should().Be(3600);
            response.TokenType.Should().Be("Bearer");
            response.IdentityToken.Should().BeNull();
            response.RefreshToken.Should().BeNull();

            var payload = GetPayload(response);

            payload.Count().Should().Be(8);
            payload.Should().Contain("iss", "https://idsvr4");
            payload.Should().Contain("client_id", "client");
            payload.Keys.Should().Contain("jti");
            payload.Keys.Should().Contain("iat");

            var audiences = ((JArray)payload["aud"]).Select(x => x.ToString());
            audiences.Count().Should().Be(2);
            audiences.Should().Contain("api");
            audiences.Should().Contain("other_api");

            var scopes = payload["scope"] as JArray;
            scopes.First().ToString().Should().Be("api1");
        }

        [Fact]
        public async Task Valid_request_with_confirmation_should_return_expected_payload()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "client.cnf",
                ClientSecret = "foo",
                Scope = "api1"
            });

            response.IsError.Should().Be(false);
            response.ExpiresIn.Should().Be(3600);
            response.TokenType.Should().Be("Bearer");
            response.IdentityToken.Should().BeNull();
            response.RefreshToken.Should().BeNull();

            var payload = GetPayload(response);

            payload.Count().Should().Be(9);
            payload.Should().Contain("iss", "https://idsvr4");
            payload.Should().Contain("client_id", "client.cnf");
            payload.Keys.Should().Contain("jti");
            payload.Keys.Should().Contain("iat");

            payload["aud"].Should().Be("api");

            var scopes = payload["scope"] as JArray;
            scopes.First().ToString().Should().Be("api1");

            var cnf = payload["cnf"] as JObject;
            cnf["x5t#S256"].ToString().Should().Be("foo");
        }

        [Fact]
        public async Task Requesting_multiple_scopes_should_return_expected_payload()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret",
                Scope = "api1 api2"
            });

            response.IsError.Should().Be(false);
            response.ExpiresIn.Should().Be(3600);
            response.TokenType.Should().Be("Bearer");
            response.IdentityToken.Should().BeNull();
            response.RefreshToken.Should().BeNull();

            var payload = GetPayload(response);

            payload.Count().Should().Be(8);
            payload.Should().Contain("iss", "https://idsvr4");
            payload.Should().Contain("client_id", "client");
            payload.Keys.Should().Contain("jti");
            payload.Keys.Should().Contain("iat");
            
            payload["aud"].Should().Be("api");

            var scopes = payload["scope"] as JArray;
            scopes.Count().Should().Be(2);
            scopes.First().ToString().Should().Be("api1");
            scopes.Skip(1).First().ToString().Should().Be("api2");
        }

        [Fact]
        public async Task Request_with_no_explicit_scopes_should_return_expected_payload()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret"
            });

            response.IsError.Should().Be(false);
            response.ExpiresIn.Should().Be(3600);
            response.TokenType.Should().Be("Bearer");
            response.IdentityToken.Should().BeNull();
            response.RefreshToken.Should().BeNull();

            var payload = GetPayload(response);

            payload.Count().Should().Be(8);
            payload.Should().Contain("iss", "https://idsvr4");
            payload.Should().Contain("client_id", "client");
            payload.Keys.Should().Contain("jti");
            payload.Keys.Should().Contain("iat");

            var audiences = ((JArray)payload["aud"]).Select(x => x.ToString());
            audiences.Count().Should().Be(2);
            audiences.Should().Contain("api");
            audiences.Should().Contain("other_api");

            var scopes = ((JArray)payload["scope"]).Select(x => x.ToString());
            scopes.Count().Should().Be(3);
            scopes.Should().Contain("api1");
            scopes.Should().Contain("api2");
            scopes.Should().Contain("other_api");
        }

        [Fact]
        public async Task Client_without_default_scopes_skipping_scope_parameter_should_return_error()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "client.no_default_scopes",
                ClientSecret = "secret"
            });

            response.IsError.Should().Be(true);
            response.ExpiresIn.Should().Be(0);
            response.TokenType.Should().BeNull();
            response.IdentityToken.Should().BeNull();
            response.RefreshToken.Should().BeNull();
            response.Error.Should().Be(OidcConstants.TokenErrors.InvalidScope);
        }

        [Fact]
        public async Task Request_posting_client_secret_in_body_should_succeed()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret",
                Scope = "api1",

                ClientCredentialStyle = ClientCredentialStyle.PostBody
            });

            response.IsError.Should().Be(false);
            response.ExpiresIn.Should().Be(3600);
            response.TokenType.Should().Be("Bearer");
            response.IdentityToken.Should().BeNull();
            response.RefreshToken.Should().BeNull();

            var payload = GetPayload(response);

            payload.Should().Contain("iss", "https://idsvr4");
            payload.Should().Contain("client_id", "client");

            payload["aud"].Should().Be("api");

            var scopes = payload["scope"] as JArray;
            scopes.First().ToString().Should().Be("api1");
        }


        [Fact]
        public async Task Request_For_client_with_no_secret_and_basic_authentication_should_succeed()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "client.no_secret",
                Scope = "api1"
            });

            response.IsError.Should().Be(false);
            response.ExpiresIn.Should().Be(3600);
            response.TokenType.Should().Be("Bearer");
            response.IdentityToken.Should().BeNull();
            response.RefreshToken.Should().BeNull();

            var payload = GetPayload(response);
            
            payload.Should().Contain("iss", "https://idsvr4");
            payload.Should().Contain("client_id", "client.no_secret");

            payload["aud"].Should().Be("api");

            var scopes = payload["scope"] as JArray;
            scopes.First().ToString().Should().Be("api1");
        }

        [Fact]
        public async Task Request_with_invalid_client_secret_should_fail()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "client",
                ClientSecret = "invalid",
                Scope = "api1"
            });

            response.IsError.Should().Be(true);
            response.Error.Should().Be("invalid_client");
        }

        [Fact]
        public async Task Unknown_client_should_fail()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "invalid",
                ClientSecret = "secret",
                Scope = "api1"
            });

            response.IsError.Should().Be(true);
            response.ErrorType.Should().Be(ResponseErrorType.Protocol);
            response.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Error.Should().Be("invalid_client");
        }

        [Fact]
        public async Task Implicit_client_should_not_use_client_credential_grant()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "implicit",
                Scope = "api1"
            });

            response.IsError.Should().Be(true);
            response.ErrorType.Should().Be(ResponseErrorType.Protocol);
            response.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Error.Should().Be("unauthorized_client");
        }

        [Fact]
        public async Task Implicit_and_client_creds_client_should_not_use_client_credential_grant_without_secret()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "implicit_and_client_creds",
                ClientSecret = "invalid",
                Scope = "api1"
            });

            response.IsError.Should().Be(true);
            response.ErrorType.Should().Be(ResponseErrorType.Protocol);
            response.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Error.Should().Be("invalid_client");
        }


        [Fact]
        public async Task Requesting_unknown_scope_should_fail()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret",
                Scope = "unknown"
            });

            response.IsError.Should().Be(true);
            response.ErrorType.Should().Be(ResponseErrorType.Protocol);
            response.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Error.Should().Be("invalid_scope");
        }

        [Fact]
        public async Task Client_explicitly_requesting_identity_scope_should_fail()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "client.identityscopes",
                ClientSecret = "secret",
                Scope = "openid api1"
            });

            response.IsError.Should().Be(true);
            response.ErrorType.Should().Be(ResponseErrorType.Protocol);
            response.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Error.Should().Be("invalid_scope");
        }

        [Fact]
        public async Task Client_explicitly_requesting_offline_access_should_fail()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret",
                Scope = "api1 offline_access"
            });

            response.IsError.Should().Be(true);
            response.ErrorType.Should().Be(ResponseErrorType.Protocol);
            response.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Error.Should().Be("invalid_scope");
        }

        [Fact]
        public async Task Requesting_unauthorized_scope_should_fail()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret",
                Scope = "api3"
            });

            response.IsError.Should().Be(true);
            response.ErrorType.Should().Be(ResponseErrorType.Protocol);
            response.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Error.Should().Be("invalid_scope");
        }

        [Fact]
        public async Task Requesting_authorized_and_unauthorized_scopes_should_fail()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret",
                Scope = "api1 api3"
            });

            response.IsError.Should().Be(true);
            response.ErrorType.Should().Be(ResponseErrorType.Protocol);
            response.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Error.Should().Be("invalid_scope");
        }

        private Dictionary<string, object> GetPayload(TokenResponse response)
        {
            var token = response.AccessToken.Split('.').Skip(1).Take(1).First();
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                Encoding.UTF8.GetString(Base64Url.Decode(token)));

            return dictionary;
        }
    }
}