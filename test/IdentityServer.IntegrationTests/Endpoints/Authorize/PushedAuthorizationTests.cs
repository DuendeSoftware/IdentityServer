using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Test;
using FluentAssertions;
using IdentityModel;
using IdentityModel.Client;
using IntegrationTests.Common;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

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
        await _mockPipeline.LoginAsync("bob");

        _mockPipeline.BrowserClient.AllowAutoRedirect = false;

        var parResponse = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.ParEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", "client1" },
                { "client_secret", "secret" },
                { "response_type", "id_token" },
                { "scope", "openid profile" },
                { "redirect_uri", "https://client1/callback" },
                { "nonce", "123_nonce" },
                { "state", "123_state" }

            }));

        parResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var parJson = await parResponse.Content.ReadFromJsonAsync<PushedAuthorizationResponse>();


        var authorizeUrl = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            extra: new
            {
                request_uri = parJson.RequestUri
            });

        var response = await _mockPipeline.BrowserClient.GetAsync(authorizeUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location.ToString().Should().StartWith("https://client1/callback");

        var authorization = new IdentityModel.Client.AuthorizeResponse(response.Headers.Location.ToString());
        authorization.IsError.Should().BeFalse();
        authorization.IdentityToken.Should().NotBeNull();
        authorization.State.Should().Be("123_state");
    }

    [Theory]
    [InlineData("urn:ietf:params:oauth:request_uri:foo")]
    [InlineData("https://requests.example.com/bar")]
    [InlineData("nonsense")]
    public async Task pushed_authorization_with_a_request_uri_fails(string requestUri)
    {
        var parResponse = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.ParEndpoint,
        new FormUrlEncodedContent(new Dictionary<string, string>
        {
                    { "client_id", "client1" },
                    { "client_secret", "secret" },
                    { "request_uri", requestUri }
        }));

        parResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await parResponse.Content.ReadFromJsonAsync<PushedAuthorizationFailure>();
        error?.Error.Should().Be(OidcConstants.AuthorizeErrors.InvalidRequest);
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

// TODO - Move to IdentityModel
public class PushedAuthorizationRequest
{ }

// TODO - Move to IdentityModel
public class PushedAuthorizationResponse
{
    [JsonPropertyName("request_uri")]
    public string RequestUri { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

