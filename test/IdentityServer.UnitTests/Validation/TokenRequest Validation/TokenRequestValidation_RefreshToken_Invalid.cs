// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Specialized;
using System.Security.Claims;
using System.Threading.Tasks;
using Duende.IdentityServer;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;
using FluentAssertions;
using IdentityModel;
using UnitTests.Common;
using UnitTests.Validation.Setup;
using Xunit;

namespace UnitTests.Validation.TokenRequest_Validation
{
    public class TokenRequestValidation_RefreshToken_Invalid
    {
        private const string Category = "TokenRequest Validation - RefreshToken - Invalid";

        private IClientStore _clients = Factory.CreateClientStore();

        [Fact]
        [Trait("Category", Category)]
        public async Task Non_existing_RefreshToken()
        {
            var client = await _clients.FindEnabledClientByIdAsync("roclient");

            var validator = Factory.CreateTokenRequestValidator();

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, "refresh_token");
            parameters.Add(OidcConstants.TokenRequest.RefreshToken, "nonexistent");

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidGrant);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task RefreshTokenTooLong()
        {
            var client = await _clients.FindEnabledClientByIdAsync("roclient");
            var options = new IdentityServerOptions();

            var validator = Factory.CreateTokenRequestValidator();
            var longRefreshToken = "x".Repeat(options.InputLengthRestrictions.RefreshToken + 1);

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, "refresh_token");
            parameters.Add(OidcConstants.TokenRequest.RefreshToken, longRefreshToken);

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidGrant);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Expired_RefreshToken()
        {
            var refreshToken = new RefreshToken
            {
                Lifetime = 10,
                CreationTime = DateTime.UtcNow.AddSeconds(-15)
            };

            var grants = Factory.CreateRefreshTokenStore();
            var handle = await grants.StoreRefreshTokenAsync(refreshToken);

            var client = await _clients.FindEnabledClientByIdAsync("roclient");

            var validator = Factory.CreateTokenRequestValidator(
                refreshTokenStore: grants);

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, "refresh_token");
            parameters.Add(OidcConstants.TokenRequest.RefreshToken, handle);

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidGrant);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Wrong_Client_Binding_RefreshToken_Request()
        {
            var refreshToken = new RefreshToken
            {
                ClientId = "otherclient"
            };

            var grants = Factory.CreateRefreshTokenStore();
            var handle = await grants.StoreRefreshTokenAsync(refreshToken);

            var client = await _clients.FindEnabledClientByIdAsync("roclient");

            var validator = Factory.CreateTokenRequestValidator(
                refreshTokenStore: grants);

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, "refresh_token");
            parameters.Add(OidcConstants.TokenRequest.RefreshToken, handle);

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidGrant);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Client_has_no_OfflineAccess_Scope_anymore_at_RefreshToken_Request()
        {
            var refreshToken = new RefreshToken
            {
                ClientId = "roclient_restricted",
                Lifetime = 600,
                CreationTime = DateTime.UtcNow
            };

            var grants = Factory.CreateRefreshTokenStore();
            var handle = await grants.StoreRefreshTokenAsync(refreshToken);

            var client = await _clients.FindEnabledClientByIdAsync("roclient_restricted");

            var validator = Factory.CreateTokenRequestValidator(
                refreshTokenStore: grants);

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, "refresh_token");
            parameters.Add(OidcConstants.TokenRequest.RefreshToken, handle);

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidGrant);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task RefreshToken_Request_with_disabled_User()
        {
            var subjectClaim = new Claim(JwtClaimTypes.Subject, "foo");

            var refreshToken = new RefreshToken
            {
                ClientId = "roclient",
                Subject = new IdentityServerUser("foo").CreatePrincipal(),
                Lifetime = 600,
                CreationTime = DateTime.UtcNow
            };

            var grants = Factory.CreateRefreshTokenStore();
            var handle = await grants.StoreRefreshTokenAsync(refreshToken);

            var client = await _clients.FindEnabledClientByIdAsync("roclient");

            var validator = Factory.CreateTokenRequestValidator(
                refreshTokenStore: grants,
                profile: new TestProfileService(shouldBeActive: false));

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, "refresh_token");
            parameters.Add(OidcConstants.TokenRequest.RefreshToken, handle);

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidGrant);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task invalid_resource_indicator()
        {
            var refreshToken = new RefreshToken
            {
                ClientId = "roclient",
                Subject = new IdentityServerUser("foo").CreatePrincipal(),
                Lifetime = 600,
                CreationTime = DateTime.UtcNow,
                AuthorizedScopes = new[] { "scope1" }
            };

            var grants = Factory.CreateRefreshTokenStore();
            var handle = await grants.StoreRefreshTokenAsync(refreshToken);

            var client = await _clients.FindEnabledClientByIdAsync("roclient");

            var validator = Factory.CreateTokenRequestValidator(refreshTokenStore: grants);

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, "refresh_token");
            parameters.Add(OidcConstants.TokenRequest.RefreshToken, handle);

            {
                parameters[OidcConstants.TokenRequest.Resource] = "urn:api1" + new string('x', 512);
                var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

                result.IsError.Should().BeTrue();
                result.Error.Should().Be(OidcConstants.TokenErrors.InvalidTarget);
            }
            {
                parameters[OidcConstants.TokenRequest.Resource] = "api";

                var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());
                result.IsError.Should().BeTrue();
                result.Error.Should().Be("invalid_target");
            }
            {
                parameters[OidcConstants.TokenRequest.Resource] = "urn:api1";
                parameters.Add(OidcConstants.TokenRequest.Resource, "urn:api2");

                var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());
                result.IsError.Should().BeTrue();
                result.Error.Should().Be("invalid_target");
            }
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task failed_resource_validation_should_fail()
        {
            var mockResourceValidator = new MockResourceValidator();
            var grants = Factory.CreateRefreshTokenStore();
            var client = (await _clients.FindEnabledClientByIdAsync("roclient")).ToValidationResult();
            
            var validator = Factory.CreateTokenRequestValidator(refreshTokenStore: grants, resourceValidator: mockResourceValidator);

            {
                var refreshToken = new RefreshToken
                {
                    ClientId = "roclient",
                    Subject = new IdentityServerUser("foo").CreatePrincipal(),
                    Lifetime = 600,
                    CreationTime = DateTime.UtcNow,
                    AuthorizedScopes = new[] { "scope1" }
                };
                var handle = await grants.StoreRefreshTokenAsync(refreshToken);

                var parameters = new NameValueCollection();
                parameters.Add(OidcConstants.TokenRequest.GrantType, "refresh_token");
                parameters.Add(OidcConstants.TokenRequest.RefreshToken, handle);
                parameters.Add("resource", "urn:api1");

                mockResourceValidator.Result = new ResourceValidationResult
                {
                    InvalidScopes = { "foo" }
                };
                var result = await validator.ValidateRequestAsync(parameters, client);

                result.IsError.Should().BeTrue();
                result.Error.Should().Be("invalid_scope");
            }

            {
                var refreshToken = new RefreshToken
                {
                    ClientId = "roclient",
                    Subject = new IdentityServerUser("foo").CreatePrincipal(),
                    Lifetime = 600,
                    CreationTime = DateTime.UtcNow,
                    AuthorizedScopes = new[] { "scope1" }
                };
                var handle = await grants.StoreRefreshTokenAsync(refreshToken);

                var parameters = new NameValueCollection();
                parameters.Add(OidcConstants.TokenRequest.GrantType, "refresh_token");
                parameters.Add(OidcConstants.TokenRequest.RefreshToken, handle);
                parameters.Add("resource", "urn:api1"); 
                
                mockResourceValidator.Result = new ResourceValidationResult
                {
                    InvalidResourceIndicators = { "foo" }
                };
                var result = await validator.ValidateRequestAsync(parameters, client);

                result.IsError.Should().BeTrue();
                result.Error.Should().Be("invalid_target");
            }
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task resource_indicator_requested_not_in_original_request_should_fail()
        {
            var grants = Factory.CreateRefreshTokenStore();
            var client = (await _clients.FindEnabledClientByIdAsync("roclient")).ToValidationResult();

            var validator = Factory.CreateTokenRequestValidator(refreshTokenStore: grants);

            var refreshToken = new RefreshToken
            {
                ClientId = "roclient",
                Subject = new IdentityServerUser("foo").CreatePrincipal(),
                Lifetime = 600,
                CreationTime = DateTime.UtcNow,
                AuthorizedScopes = new[] { "scope1" },
                AuthorizedResourceIndicators = new[] { "urn:api1", "urn:api2" }
            };
            var handle = await grants.StoreRefreshTokenAsync(refreshToken);

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, "refresh_token");
            parameters.Add(OidcConstants.TokenRequest.RefreshToken, handle);
            parameters.Add("resource", "urn:api3");

            var result = await validator.ValidateRequestAsync(parameters, client);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_target");
        }
    }
}