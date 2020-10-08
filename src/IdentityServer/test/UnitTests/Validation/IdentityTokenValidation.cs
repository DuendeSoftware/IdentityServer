// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityModel;
using IdentityServer.UnitTests.Validation.Setup;
using Xunit;

namespace IdentityServer.UnitTests.Validation
{
    public class IdentityTokenValidation
    {
        private const string Category = "Identity token validation";

        static IdentityTokenValidation()
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Valid_IdentityToken_DefaultKeyType()
        {
            var creator = Factory.CreateDefaultTokenCreator();
            var token = TokenFactory.CreateIdentityToken("roclient", "valid");
            var jwt = await creator.CreateTokenAsync(token);

            var validator = Factory.CreateTokenValidator();
            var result = await validator.ValidateIdentityTokenAsync(jwt, "roclient");

            result.IsError.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Valid_IdentityToken_DefaultKeyType_no_ClientId_supplied()
        {
            var creator = Factory.CreateDefaultTokenCreator();
            var jwt = await creator.CreateTokenAsync(TokenFactory.CreateIdentityToken("roclient", "valid"));
            var validator = Factory.CreateTokenValidator();

            var result = await validator.ValidateIdentityTokenAsync(jwt, "roclient");
            result.IsError.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task Valid_IdentityToken_no_ClientId_supplied()
        {
            var creator = Factory.CreateDefaultTokenCreator();
            var jwt = await creator.CreateTokenAsync(TokenFactory.CreateIdentityToken("roclient", "valid"));
            var validator = Factory.CreateTokenValidator();

            var result = await validator.ValidateIdentityTokenAsync(jwt);
            result.IsError.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task IdentityToken_InvalidClientId()
        {
            var creator = Factory.CreateDefaultTokenCreator();
            var jwt = await creator.CreateTokenAsync(TokenFactory.CreateIdentityToken("roclient", "valid"));
            var validator = Factory.CreateTokenValidator();

            var result = await validator.ValidateIdentityTokenAsync(jwt, "invalid");
            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.ProtectedResourceErrors.InvalidToken);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task IdentityToken_Too_Long()
        {
            var creator = Factory.CreateDefaultTokenCreator();
            var jwt = await creator.CreateTokenAsync(TokenFactory.CreateIdentityTokenLong("roclient", "valid", 1000));
            var validator = Factory.CreateTokenValidator();

            var result = await validator.ValidateIdentityTokenAsync(jwt, "roclient");
            result.IsError.Should().BeTrue();
            result.Error.Should().Be(OidcConstants.ProtectedResourceErrors.InvalidToken);
        }
    }
}