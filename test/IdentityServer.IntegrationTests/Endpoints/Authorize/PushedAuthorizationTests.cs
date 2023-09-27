using Duende.IdentityServer.Models;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Test;
using FluentAssertions;
using IdentityModel;
using IntegrationTests.Common;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
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

        _mockPipeline.Options.Endpoints.EnablePushedAuthorizationEndpoint = true;
    }

    [Fact]
    public async Task happy_path()
    {
        // Login
        await _mockPipeline.LoginAsync("bob");
        _mockPipeline.BrowserClient.AllowAutoRedirect = false;

        // Push Authorization
        var expectedCallback = _client.RedirectUris.First();
        var expectedState = "123_state";
        var (parJson, statusCode) = await _mockPipeline.PushAuthorizationRequestAsync(
            redirectUri: expectedCallback,
            state: expectedState
        );
        var parSuccess = parJson as PushedAuthorizationSuccess;
        statusCode.Should().Be(HttpStatusCode.Created);

        // Authorize using pushed request
        var authorizeUrl = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            extra: new
            {
                request_uri = parSuccess.RequestUri
            });
        var response = await _mockPipeline.BrowserClient.GetAsync(authorizeUrl);

        response.Should().Be302Found();
        response.Should().HaveHeader("Location").And.Match($"{expectedCallback}*");

        var authorization = new IdentityModel.Client.AuthorizeResponse(response.Headers.Location.ToString());
        authorization.IsError.Should().BeFalse();
        authorization.IdentityToken.Should().NotBeNull();
        authorization.State.Should().Be(expectedState);
    }

    [Fact]
    public async Task using_pushed_authorization_when_it_is_globally_disabled_fails()
    {
        _mockPipeline.Options.Endpoints.EnablePushedAuthorizationEndpoint = false;
        
        var (_, statusCode) = await _mockPipeline.PushAuthorizationRequestAsync();
        statusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task not_using_pushed_authorization_when_it_is_globally_required_fails()
    {
        _mockPipeline.Options.PushedAuthorization.Required = true;

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "id_token",
            scope: "openid",
            redirectUri: "https://client1/callback",
            nonce: "123_nonce");
        _mockPipeline.BrowserClient.AllowAutoRedirect = false;
        var response = await _mockPipeline.BrowserClient.GetAsync(url);

        // We expect to be redirected to the error page, as this is an interactive
        // call to authorize
        response.Should().Be302Found();
        response.Should().HaveHeader("Location").And.Match("*/error*"); 
    }

    [Fact]
    public async Task not_using_pushed_authorization_when_it_is_required_for_client_fails()
    {
        _mockPipeline.Options.Endpoints.EnablePushedAuthorizationEndpoint.Should().BeTrue();
        _mockPipeline.Options.PushedAuthorization.Required.Should().BeFalse();
        _client.RequirePushedAuthorization = true;

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "id_token",
            scope: "openid",
            redirectUri: "https://client1/callback",
            nonce: "123_nonce");
        _mockPipeline.BrowserClient.AllowAutoRedirect = false;
        var response = await _mockPipeline.BrowserClient.GetAsync(url);

        // We expect to be redirected to the error page, as this is an interactive
        // call to authorize
        response.Should().Be302Found();
        response.Should().HaveHeader("Location").And.Match("*/error*"); 
    }

    [Fact]
    public async Task existing_pushed_authorization_request_uris_become_invalid_when_par_is_disabled()
    {
        // PAR is enabled when we push authorization...
        var (parJson, statusCode) = await _mockPipeline.PushAuthorizationRequestAsync();
        var parSuccess = parJson as PushedAuthorizationSuccess;
        statusCode.Should().Be(HttpStatusCode.Created);
        parSuccess.Should().NotBeNull();

        // ... But then is later disabled, and then we try to use the pushed request
        _mockPipeline.Options.Endpoints.EnablePushedAuthorizationEndpoint = false;

        // Authorize using pushed request
        var authorizeUrl = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            extra: new
            {
                request_uri = parSuccess.RequestUri
            });

        // We expect to be redirected to the error page, as this is an interactive
        // call to authorize. We don't want to follow redirects. Instead we'll just
        // check for a 302 to the error page
        _mockPipeline.BrowserClient.AllowAutoRedirect = false;
        var authorizeResponse = await _mockPipeline.BrowserClient.GetAsync(authorizeUrl);

        authorizeResponse.Should().Be302Found();
        authorizeResponse.Should().HaveHeader("Location").And.Match("*/error*");
    }

    [Theory]
    [InlineData("urn:ietf:params:oauth:request_uri:foo")]
    [InlineData("https://requests.example.com/bar")]
    [InlineData("nonsense")]
    public async Task pushed_authorization_with_a_request_uri_fails(string requestUri)
    {
        var (parJson, statusCode) = await _mockPipeline.PushAuthorizationRequestAsync(
            extra: new Dictionary<string, string>
            {
                { "request_uri", requestUri }
            });
        statusCode.Should().Be(HttpStatusCode.BadRequest);
        parJson.Should().NotBeNull()
            .And.Subject.As<PushedAuthorizationFailure>().Error.Should()
            .Be(OidcConstants.AuthorizeErrors.InvalidRequest);
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
