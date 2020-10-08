using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityModel;
using IdentityServer.IntegrationTests.Common;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace IdentityServer.IntegrationTests.Extensibility
{
    public class CustomProfileServiceTests
    {
        private const string Category = "Authorize endpoint";

        private IdentityServerPipeline _mockPipeline = new IdentityServerPipeline();

        public CustomProfileServiceTests()
        {
            _mockPipeline.OnPostConfigureServices += svcs =>
            {
                svcs.AddTransient<IProfileService, CustomProfileService>();
            };

            _mockPipeline.Clients.Add(new Client
            {
                ClientId = "implicit",
                AllowedGrantTypes = GrantTypes.Implicit,
                RedirectUris = { "https://client/callback" },
                RequireConsent = false,
                AllowedScopes = { "openid", "custom_identity" }
            });

            _mockPipeline.IdentityScopes.Add(new IdentityResources.OpenId());
            _mockPipeline.IdentityScopes.Add(new IdentityResource("custom_identity", new string[] { "foo" }));

            _mockPipeline.Users.Add(new IdentityServer4.Test.TestUser
            {
                SubjectId = "bob",
                Username = "bob",
                Password = "password",
            });

            _mockPipeline.Initialize();
        }

        [Fact]
        public async Task custom_profile_should_return_claims_for_implicit_client()
        {
            await _mockPipeline.LoginAsync("bob");

            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "implicit",
                responseType: "id_token",
                scope: "openid custom_identity",
                redirectUri: "https://client/callback",
                state: "state",
                nonce: "nonce");

            _mockPipeline.BrowserClient.AllowAutoRedirect = false;
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location.ToString().Should().StartWith("https://client/callback");

            var authorization = new IdentityModel.Client.AuthorizeResponse(response.Headers.Location.ToString());
            authorization.IsError.Should().BeFalse();
            authorization.IdentityToken.Should().NotBeNull();

            var payload = authorization.IdentityToken.Split('.')[1];
            var json = Encoding.UTF8.GetString(Base64Url.Decode(payload));
            var obj = JObject.Parse(json);

            obj.GetValue("foo").Should().NotBeNull();
            obj["foo"].ToString().Should().Be("bar");
        }
    }

    public class CustomProfileService : IProfileService
    {
        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var claims = new Claim[]
            {
                new Claim("foo", "bar")
            };
            context.AddRequestedClaims(claims);
            return Task.CompletedTask;
        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            context.IsActive = true;
            return Task.CompletedTask;
        }
    }
}
