using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Test;
using FluentAssertions;
using IntegrationTests.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IdentityServer.IntegrationTests.Endpoints.Ciba
{
    public class CibaTests
    {
        private const string Category = "Backchannel Authentication (CIBA) endpoint";

        private IdentityServerPipeline _mockPipeline = new IdentityServerPipeline();

        private Client _cibaClient;

        public CibaTests()
        {
            _mockPipeline.Clients.AddRange(new Client[] {
                _cibaClient = new Client
                {
                    ClientId = "client1",
                    AllowedGrantTypes = GrantTypes.Ciba,
                    RequireConsent = false,
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256()),
                        new Secret
                        {
                            Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                            Value = "{'e':'AQAB','kid':'ZzAjSnraU3bkWGnnAqLapYGpTyNfLbjbzgAPbbW2GEA','kty':'RSA','n':'wWwQFtSzeRjjerpEM5Rmqz_DsNaZ9S1Bw6UbZkDLowuuTCjBWUax0vBMMxdy6XjEEK4Oq9lKMvx9JzjmeJf1knoqSNrox3Ka0rnxXpNAz6sATvme8p9mTXyp0cX4lF4U2J54xa2_S9NF5QWvpXvBeC4GAJx7QaSw4zrUkrc6XyaAiFnLhQEwKJCwUw4NOqIuYvYp_IXhw-5Ti_icDlZS-282PcccnBeOcX7vc21pozibIdmZJKqXNsL1Ibx5Nkx1F1jLnekJAmdaACDjYRLL_6n3W4wUp19UvzB1lGtXcJKLLkqB6YDiZNu16OSiSprfmrRXvYmvD8m6Fnl5aetgKw'}"
                        }
                    },
                    AllowOfflineAccess = true,
                    AllowedScopes = new List<string> { "openid", "profile", "scope1" },
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
                    Scopes = { "scope1" }
                }
            });
            _mockPipeline.ApiScopes.AddRange(new ApiScope[] {
                new ApiScope
                {
                    Name = "scope1"
                },
            });

            _mockPipeline.Initialize();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task get_request_should_return_error()
        {
            var response = await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.BackchannelAuthenticationEndpoint);

            response.StatusCode.Should().NotBe(HttpStatusCode.BadRequest);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task post_request_without_form_should_return_error()
        {
            var response = await _mockPipeline.BrowserClient.PostAsync(IdentityServerPipeline.BackchannelAuthenticationEndpoint, new StringContent("foo"));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }


    }
}
