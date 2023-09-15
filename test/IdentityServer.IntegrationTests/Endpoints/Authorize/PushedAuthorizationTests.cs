using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Test;
using FluentAssertions;
using IntegrationTests.Common;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IntegrationTests.Endpoints.Authorize;

public class PushedAuthorizationTests
{
    private readonly IdentityServerPipeline _mockPipeline = new();
    private Client _client;


    public PushedAuthorizationTests()
    {
        ConfigureClients();
        ConfigureUsers();
        ConfigureScopesAndResources();

        _mockPipeline.Initialize();

        _mockPipeline.Options.PushedAuthorization.Enabled = true;
    }

    [Fact]
    public async Task happy_path()
    {
        var response = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.ParEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", "client1" },
                { "client_secret", "secret" },
                { "response_type", "id_token" },
                { "scope", "openid profile" },
                { "redirect_uri", "https://client1/callback" },
                { "nonce", "123_nonce" }
            }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }


    private void ConfigureScopesAndResources()
    {
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
    }

    private void ConfigureUsers()
    {
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
    }

    private void ConfigureClients()
    {
        _mockPipeline.Clients.AddRange(new Client[]
        {
            _client = new Client
            {
                ClientId = "client1",
                ClientSecrets = new []
                {
                     new Secret("secret".Sha256())
                },
                AllowedGrantTypes = GrantTypes.Implicit,
                RequireConsent = false,
                RequirePkce = false,
                AllowedScopes = new List<string> { "openid", "profile" },
                RedirectUris = new List<string> { "https://client1/callback" },
            },
        });
    }
}
