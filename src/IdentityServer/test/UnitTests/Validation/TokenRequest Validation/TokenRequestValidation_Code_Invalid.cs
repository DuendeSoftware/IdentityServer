// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityModel;
using IdentityServer.UnitTests.Common;
using IdentityServer.UnitTests.Validation.Setup;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Xunit;

namespace IdentityServer.UnitTests.Validation.TokenRequest_Validation
{
    public class TokenRequestValidation_Code_Invalid
    {
        private IClientStore _clients = Factory.CreateClientStore();
        private const string Category = "TokenRequest Validation - AuthorizationCode - Invalid";

        private ClaimsPrincipal _subject = new IdentityServerUser("bob").CreatePrincipal();

        [Fact]
        [Trait("Category", Category)]
        public async Task Missing_AuthorizationCode()
        {
            var client = await _clients.FindEnabledClientByIdAsync("codeclient");
            var store = Factory.CreateAuthorizationCodeStore();

            var code = new AuthorizationCode
            {
                CreationTime = DateTime.UtcNow,
                ClientId = client.ClientId,
                Lifetime = client.AuthorizationCodeLifetime,
                IsOpenId = true,
                RedirectUri = "https://server/cb",
                Subject = _subject
            };

            var handle = await store.StoreAuthorizationCodeAsync(code);

            var validator = Factory.CreateTokenRequestValidator(
                authorizationCodeStore: store);

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.AuthorizationCode);
            parameters.Add(OidcConstants.TokenRequest.RedirectUri, "https://server/cb");

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidGrant);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Invalid_AuthorizationCode()
        {
            var client = await _clients.FindEnabledClientByIdAsync("codeclient");
            var store = Factory.CreateAuthorizationCodeStore();

            var code = new AuthorizationCode
            {
                CreationTime = DateTime.UtcNow,
                ClientId = client.ClientId,
                Lifetime = client.AuthorizationCodeLifetime,
                IsOpenId = true,
                RedirectUri = "https://server/cb",
                Subject = _subject
            };

            var handle = await store.StoreAuthorizationCodeAsync(code);

            var validator = Factory.CreateTokenRequestValidator(
                authorizationCodeStore: store);

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.AuthorizationCode);
            parameters.Add(OidcConstants.TokenRequest.Code, "invalid");
            parameters.Add(OidcConstants.TokenRequest.RedirectUri, "https://server/cb");

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidGrant);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task AuthorizationCodeTooLong()
        {
            var client = await _clients.FindEnabledClientByIdAsync("codeclient");
            var store = Factory.CreateAuthorizationCodeStore();
            var options = new IdentityServerOptions();

            var code = new AuthorizationCode
            {
                CreationTime = DateTime.UtcNow,
                ClientId = client.ClientId,
                Lifetime = client.AuthorizationCodeLifetime,
                IsOpenId = true,
                RedirectUri = "https://server/cb",
                Subject = _subject
            };

            var handle = await store.StoreAuthorizationCodeAsync(code);

            var validator = Factory.CreateTokenRequestValidator(
                authorizationCodeStore: store);
            var longCode = "x".Repeat(options.InputLengthRestrictions.AuthorizationCode + 1);

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.AuthorizationCode);
            parameters.Add(OidcConstants.TokenRequest.Code, longCode);
            parameters.Add(OidcConstants.TokenRequest.RedirectUri, "https://server/cb");

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidGrant);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task No_Scopes_for_AuthorizationCode()
        {
            var client = await _clients.FindEnabledClientByIdAsync("codeclient");
            var store = Factory.CreateAuthorizationCodeStore();

            var code = new AuthorizationCode
            {
                CreationTime = DateTime.UtcNow,
                ClientId = client.ClientId,
                Lifetime = client.AuthorizationCodeLifetime,
                IsOpenId = true,
                RedirectUri = "https://server/cb",
                Subject = _subject
            };

            var handle = await store.StoreAuthorizationCodeAsync(code);

            var validator = Factory.CreateTokenRequestValidator(
                authorizationCodeStore: store);

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.AuthorizationCode);
            parameters.Add(OidcConstants.TokenRequest.Code, handle);
            parameters.Add(OidcConstants.TokenRequest.RedirectUri, "https://server/cb");

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            OidcConstants.TokenErrors.InvalidRequest.Should().Be(result.Error);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Client_Not_Authorized_For_AuthorizationCode_Flow()
        {
            var client = await _clients.FindEnabledClientByIdAsync("implicitclient");
            var store = Factory.CreateAuthorizationCodeStore();

            var code = new AuthorizationCode
            {
                CreationTime = DateTime.UtcNow,
                ClientId = client.ClientId,
                Lifetime = client.AuthorizationCodeLifetime,
                IsOpenId = true,
                RedirectUri = "https://server/cb",
                Subject = _subject
            };

            var handle = await store.StoreAuthorizationCodeAsync(code);

            var validator = Factory.CreateTokenRequestValidator(
                authorizationCodeStore: store);

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.AuthorizationCode);
            parameters.Add(OidcConstants.TokenRequest.Code, handle);
            parameters.Add(OidcConstants.TokenRequest.RedirectUri, "https://server/cb");

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.UnauthorizedClient);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Client_Trying_To_Request_Token_Using_Another_Clients_Code()
        {
            var client1 = await _clients.FindEnabledClientByIdAsync("codeclient");
            var client2 = await _clients.FindEnabledClientByIdAsync("codeclient_restricted");
            var store = Factory.CreateAuthorizationCodeStore();

            var code = new AuthorizationCode
            {
                CreationTime = DateTime.UtcNow,
                ClientId = client1.ClientId,
                Lifetime = client1.AuthorizationCodeLifetime,
                IsOpenId = true,
                RedirectUri = "https://server/cb",
                Subject = _subject
            };

            var handle = await store.StoreAuthorizationCodeAsync(code);

            var validator = Factory.CreateTokenRequestValidator(
                authorizationCodeStore: store);

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.AuthorizationCode);
            parameters.Add(OidcConstants.TokenRequest.Code, handle);
            parameters.Add(OidcConstants.TokenRequest.RedirectUri, "https://server/cb");

            var result = await validator.ValidateRequestAsync(parameters, client2.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidGrant);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Missing_RedirectUri()
        {
            var client = await _clients.FindEnabledClientByIdAsync("codeclient");
            var store = Factory.CreateAuthorizationCodeStore();

            var code = new AuthorizationCode
            {
                CreationTime = DateTime.UtcNow,
                ClientId = client.ClientId,
                Lifetime = client.AuthorizationCodeLifetime,
                IsOpenId = true,
                RedirectUri = "https://server/cb",
                Subject = _subject
            };

            var handle = await store.StoreAuthorizationCodeAsync(code);

            var validator = Factory.CreateTokenRequestValidator(
                authorizationCodeStore: store);

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.AuthorizationCode);
            parameters.Add(OidcConstants.TokenRequest.Code, handle);

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.UnauthorizedClient);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Different_RedirectUri_Between_Authorize_And_Token_Request()
        {
            var client = await _clients.FindEnabledClientByIdAsync("codeclient");
            var store = Factory.CreateAuthorizationCodeStore();

            var code = new AuthorizationCode
            {
                CreationTime = DateTime.UtcNow,
                ClientId = client.ClientId,
                Lifetime = client.AuthorizationCodeLifetime,
                IsOpenId = true,
                RedirectUri = "https://server1/cb",
                Subject = _subject
            };

            var handle = await store.StoreAuthorizationCodeAsync(code);

            var validator = Factory.CreateTokenRequestValidator(
                authorizationCodeStore: store);

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.AuthorizationCode);
            parameters.Add(OidcConstants.TokenRequest.Code, handle);
            parameters.Add(OidcConstants.TokenRequest.RedirectUri, "https://server2/cb");

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidGrant);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Expired_AuthorizationCode()
        {
            var client = await _clients.FindEnabledClientByIdAsync("codeclient");
            var store = Factory.CreateAuthorizationCodeStore();

            var code = new AuthorizationCode
            {
                ClientId = client.ClientId,
                Lifetime = client.AuthorizationCodeLifetime,
                IsOpenId = true,
                RedirectUri = "https://server/cb",
                CreationTime = DateTime.UtcNow.AddSeconds(-100),
                Subject = _subject
            };

            var handle = await store.StoreAuthorizationCodeAsync(code);

            var validator = Factory.CreateTokenRequestValidator(
                authorizationCodeStore: store);

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.AuthorizationCode);
            parameters.Add(OidcConstants.TokenRequest.Code, handle);
            parameters.Add(OidcConstants.TokenRequest.RedirectUri, "https://server/cb");

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidGrant);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Reused_AuthorizationCode()
        {
            var client = await _clients.FindEnabledClientByIdAsync("codeclient");
            var store = Factory.CreateAuthorizationCodeStore();

            var code = new AuthorizationCode
            {
                CreationTime = DateTime.UtcNow,
                Subject = new IdentityServerUser("123").CreatePrincipal(),
                ClientId = client.ClientId,
                Lifetime = client.AuthorizationCodeLifetime,
                IsOpenId = true,
                RedirectUri = "https://server/cb",
                RequestedScopes = new List<string>
                {
                    "openid"
                }
            };

            var handle = await store.StoreAuthorizationCodeAsync(code);

            var validator = Factory.CreateTokenRequestValidator(
                authorizationCodeStore: store);

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.AuthorizationCode);
            parameters.Add(OidcConstants.TokenRequest.Code, handle);
            parameters.Add(OidcConstants.TokenRequest.RedirectUri, "https://server/cb");

            // request first time
            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeFalse();

            // request second time
            validator = Factory.CreateTokenRequestValidator(
                authorizationCodeStore: store);
            
            result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidGrant);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Code_Request_with_disabled_User()
        {
            var client = await _clients.FindEnabledClientByIdAsync("codeclient");
            var store = Factory.CreateAuthorizationCodeStore();

            var code = new AuthorizationCode
            {
                CreationTime = DateTime.UtcNow,
                ClientId = client.ClientId,
                Lifetime = client.AuthorizationCodeLifetime,
                Subject = new IdentityServerUser("123").CreatePrincipal(),
                RedirectUri = "https://server/cb",
                RequestedScopes = new List<string>
                {
                    "openid"
                }
            };

            var handle = await store.StoreAuthorizationCodeAsync(code);

            var validator = Factory.CreateTokenRequestValidator(
                authorizationCodeStore: store,
                profile: new TestProfileService(shouldBeActive: false));

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.AuthorizationCode);
            parameters.Add(OidcConstants.TokenRequest.Code, handle);
            parameters.Add(OidcConstants.TokenRequest.RedirectUri, "https://server/cb");

            var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.TokenErrors.InvalidGrant);
        }
    }
}