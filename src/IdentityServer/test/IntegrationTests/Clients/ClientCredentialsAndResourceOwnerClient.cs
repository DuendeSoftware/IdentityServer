// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityModel.Client;
using IdentityServer.IntegrationTests.Clients.Setup;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace IdentityServer.IntegrationTests.Clients
{
    public class ClientCredentialsandResourceOwnerClient
    {
        private const string TokenEndpoint = "https://server/connect/token";

        private readonly HttpClient _client;

        public ClientCredentialsandResourceOwnerClient()
        {
            var builder = new WebHostBuilder()
                .UseStartup<Startup>();
            var server = new TestServer(builder);

            _client = server.CreateClient();
        }

        [Fact]
        public async Task Resource_scope_should_be_requestable_via_client_credentials()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "client.and.ro",
                ClientSecret = "secret",
                Scope = "api1"
            });

            response.IsError.Should().Be(false);
        }

        [Fact]
        public async Task Openid_scope_should_not_be_requestable_via_client_credentials()
        {
            var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "client.and.ro",
                ClientSecret = "secret",
                Scope = "openid api1"
            });

            response.IsError.Should().Be(true);
        }

        [Fact]
        public async Task Openid_scope_should_be_requestable_via_password()
        {
            var response = await _client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "client.and.ro",
                ClientSecret = "secret",
                Scope = "openid",

                UserName = "bob",
                Password = "bob"
            });

            response.IsError.Should().Be(false);
        }

        [Fact]
        public async Task Openid_and_resource_scope_should_be_requestable_via_password()
        {
            var response = await _client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = "client.and.ro",
                ClientSecret = "secret",
                Scope = "openid api1",

                UserName = "bob",
                Password = "bob"
            });

            response.IsError.Should().Be(false);
        }
    }
}