// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityModel;
using IdentityServer.UnitTests.Validation.Setup;
using IdentityServer4.Stores;
using Xunit;

namespace IdentityServer.UnitTests.Validation.TokenRequest_Validation
{
    public class TokenRequestValidation_ClientCredentials_Invalid
    {
        private const string Category = "TokenRequest Validation - ClientCredentials - Invalid";

        private IClientStore _clients = Factory.CreateClientStore();

        [Fact]
        [Trait("Category", Category)]
        public async Task Invalid_GrantType_For_Client()
        {
            var client = await _clients.FindEnabledClientByIdAsync("roclient");
            var validator = Factory.CreateTokenRequestValidator();

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials);
            parameters.Add(OidcConstants.TokenRequest.Scope, "resource");

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.UnauthorizedClient);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Request_should_succeed_even_with_allowed_identity_scopes_because_they_are_filtered_out()
        {
            var client = await _clients.FindEnabledClientByIdAsync("client");
            var validator = Factory.CreateTokenRequestValidator();

            var parameters = new NameValueCollection
            {
                { OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials }
            };

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeFalse();
            result.ValidatedRequest.ValidatedResources.Resources.ApiResources.Count.Should().Be(1);
            result.ValidatedRequest.ValidatedResources.Resources.ApiResources.First().Name.Should().Be("api");

            result.ValidatedRequest.ValidatedResources.Resources.ApiScopes.Count.Should().Be(2);
            result.ValidatedRequest.ValidatedResources.Resources.ApiScopes.Select(x=>x.Name).Should().BeEquivalentTo(new[] { "resource", "resource2" });
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Unknown_Scope()
        {
            var client = await _clients.FindEnabledClientByIdAsync("client");
            var validator = Factory.CreateTokenRequestValidator();
            
            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials);
            parameters.Add(OidcConstants.TokenRequest.Scope, "unknown");

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidScope);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Unknown_Scope_Multiple()
        {
            var client = await _clients.FindEnabledClientByIdAsync("client");
            var validator = Factory.CreateTokenRequestValidator();

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials);
            parameters.Add(OidcConstants.TokenRequest.Scope, "resource unknown");

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidScope);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Restricted_Scope()
        {
            var client = await _clients.FindEnabledClientByIdAsync("client_restricted");
            var validator = Factory.CreateTokenRequestValidator();

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials);
            parameters.Add(OidcConstants.TokenRequest.Scope, "resource2");

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidScope);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Restricted_Scope_Multiple()
        {
            var client = await _clients.FindEnabledClientByIdAsync("client_restricted");
            var validator = Factory.CreateTokenRequestValidator();

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials);
            parameters.Add(OidcConstants.TokenRequest.Scope, "resource resource2");

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidScope);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Identity_scope_is_not_allowed_for_client_credentials_when_specified_explicitly()
        {
            var client = await _clients.FindEnabledClientByIdAsync("client");
            var validator = Factory.CreateTokenRequestValidator();

            var parameters = new NameValueCollection
            {
                { OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials },
                { OidcConstants.TokenRequest.Scope, "openid" }
            };

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidScope);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Resource_and_Refresh_Token()
        {
            var client = await _clients.FindEnabledClientByIdAsync("client");
            var validator = Factory.CreateTokenRequestValidator();

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials);
            parameters.Add(OidcConstants.TokenRequest.Scope, "resource offline_access");

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidScope);
        }
    }
}