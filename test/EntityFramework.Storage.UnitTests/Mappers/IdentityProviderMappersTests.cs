// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Hosting.DynamicProviders;
using FluentAssertions;
using Xunit;

namespace UnitTests.Mappers
{
    public class IdentityProviderMappersTests
    {
        [Fact]
        public void IdentityProviderAutomapperConfigurationIsValid()
        {
            IdentityProviderMappers.Mapper.ConfigurationProvider.AssertConfigurationIsValid<IdentityProviderMapperProfile>();
        }

        [Fact]
        public void CanMapIdp()
        {
            var model = new OidcProvider();
            var mappedEntity = model.ToEntity();
            var mappedModel = mappedEntity.ToOidcModel();

            Assert.NotNull(mappedModel);
            Assert.NotNull(mappedEntity);
        }

        [Fact]
        public void Properties_Map()
        {
            var model = new OidcProvider()
            {
                Enabled = false,
                Authority = "auth",
                ClientId = "client",
                ClientSecret = "secret",
                DisplayName = "name",
                ResponseType = "rt",
                Scheme = "scheme",
                Scope = "scope",
                Type = "type"
            };


            var mappedEntity = model.ToEntity();
            mappedEntity.Authority.Should().Be("auth");
            mappedEntity.ClientId.Should().Be("client");
            mappedEntity.ClientSecret.Should().Be("secret");
            mappedEntity.DisplayName.Should().Be("name");
            mappedEntity.ResponseType.Should().Be("rt");
            mappedEntity.Scheme.Should().Be("scheme");
            mappedEntity.Scope.Should().Be("scope");
            mappedEntity.Type.Should().Be("type");


            var mappedModel = mappedEntity.ToOidcModel();

            mappedModel.Authority.Should().Be("auth");
            mappedModel.ClientId.Should().Be("client");
            mappedModel.ClientSecret.Should().Be("secret");
            mappedModel.DisplayName.Should().Be("name");
            mappedModel.ResponseType.Should().Be("rt");
            mappedModel.Scheme.Should().Be("scheme");
            mappedModel.Scope.Should().Be("scope");
            mappedModel.Type.Should().Be("type");
        }
    }
}