// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using FluentAssertions;
using Xunit;

namespace UnitTests.Mappers;

public class IdentityProviderMappersTests
{
    [Fact]
    public void IdentityProviderAutomapperConfigurationIsValid()
    {
        IdentityProviderMappers.Mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    [Fact]
    public void CanMapIdp()
    {
        var model = new OidcProvider();
        var mappedEntity = model.ToEntity();
        var mappedModel = mappedEntity.ToModel();

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
        };


        var mappedEntity = model.ToEntity();
        mappedEntity.DisplayName.Should().Be("name");
        mappedEntity.Scheme.Should().Be("scheme");
        mappedEntity.Type.Should().Be("oidc");
        mappedEntity.Properties.Should().NotBeNullOrEmpty();


        var mappedModel = new OidcProvider(mappedEntity.ToModel());

        mappedModel.Authority.Should().Be("auth");
        mappedModel.ClientId.Should().Be("client");
        mappedModel.ClientSecret.Should().Be("secret");
        mappedModel.DisplayName.Should().Be("name");
        mappedModel.ResponseType.Should().Be("rt");
        mappedModel.Scheme.Should().Be("scheme");
        mappedModel.Scope.Should().Be("scope");
        mappedModel.Type.Should().Be("oidc");
    }
}