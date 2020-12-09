// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;
using FluentAssertions;
using IdentityModel;
using UnitTests.Common;
using Xunit;

namespace UnitTests.Validation.AuthorizeRequest_Validation
{
    public class Authorize_ProtocolValidation_Resources
    {
        private const string Category = "AuthorizeRequest Protocol Validation - Resources";
        
        private readonly AuthorizeRequestValidator _subject;

        private readonly IdentityServerOptions _options = new IdentityServerOptions();
        private readonly MockResourceValidator _mockResourceValidator = new MockResourceValidator();
        private readonly MockUserSession _mockUserSession = new MockUserSession();

        private readonly List<Client> _clients = new List<Client>()
        {
            new Client{ 
                ClientId = "client1",
                RequirePkce = false,
                AllowedGrantTypes = GrantTypes.Code,
                AllowedScopes = { "openid", "scope1" },
                RedirectUris = { "https://client1" },
            }
        };

        public Authorize_ProtocolValidation_Resources()
        {
            _subject = new AuthorizeRequestValidator(
                _options,
                new InMemoryClientStore(_clients),
                new DefaultCustomAuthorizeRequestValidator(),
                new StrictRedirectUriValidator(),
                _mockResourceValidator,
                _mockUserSession,
                new JwtRequestValidator("aud", TestLogger.Create<JwtRequestValidator>()),
                new MockJwtRequestUriHttpClient(),
                TestLogger.Create<AuthorizeRequestValidator>());
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task no_resourceindicators_should_succeed()
        {
            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "client1");
            parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid scope1");
            parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://client1");
            parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);

            var result = await _subject.ValidateAsync(parameters);

            result.IsError.Should().Be(false);
            result.ValidatedRequest.RequestedResources.Should().BeEmpty();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task invalid_uri_resourceindicator_should_fail()
        {
            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "client1");
            parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid scope1");
            parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://client1");
            parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);
            parameters.Add("resource", "not_uri");

            var result = await _subject.ValidateAsync(parameters);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_target");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task valid_uri_resourceindicator_should_succeed()
        {
            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "client1");
            parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid scope1");
            parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://client1");
            parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);
            parameters.Add("resource", "http://resource1");

            var result = await _subject.ValidateAsync(parameters);

            result.IsError.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task multiple_uri_resourceindicators_should_succeed()
        {
            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "client1");
            parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid scope1");
            parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://client1");
            parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);
            parameters.Add("resource", "http://resource1");
            parameters.Add("resource", "http://resource2");
            parameters.Add("resource", "urn:test1");

            var result = await _subject.ValidateAsync(parameters);

            result.IsError.Should().BeFalse();
            result.ValidatedRequest.RequestedResources.Should()
                .BeEquivalentTo(new[] { "urn:test1", "http://resource1", "http://resource2" });
        }
        
        [Fact]
        [Trait("Category", Category)]
        public async Task failed_resource_validation_should_fail()
        {
            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "client1");
            parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid scope1");
            parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://client1");
            parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);
            parameters.Add("resource", "http://resource1");

            {
                _mockResourceValidator.Result = new ResourceValidationResult
                {
                    InvalidScopes = { "foo" }
                };
                var result = await _subject.ValidateAsync(parameters);

                result.IsError.Should().BeTrue();
                result.Error.Should().Be("invalid_scope");
            }

            {
                _mockResourceValidator.Result = new ResourceValidationResult
                {
                    InvalidResourceIndicators = { "foo" }
                };
                var result = await _subject.ValidateAsync(parameters);

                result.IsError.Should().BeTrue();
                result.Error.Should().Be("invalid_target");
            }
        }
    }
}