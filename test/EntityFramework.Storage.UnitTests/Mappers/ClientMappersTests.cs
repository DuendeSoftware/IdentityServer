// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Linq;
using Duende.IdentityServer.EntityFramework.Mappers;
using FluentAssertions;
using Xunit;
using Models = Duende.IdentityServer.Models;
using Entities = Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.Models;
using System.Collections.Generic;
using System.Reflection;

namespace UnitTests.Mappers;

public class ClientMappersTests
{
    [Fact]
    public void Can_Map()
    {
        var model = new Models.Client();
        var mappedEntity = model.ToEntity();
        var mappedModel = mappedEntity.ToModel();

        Assert.NotNull(mappedModel);
        Assert.NotNull(mappedEntity);
    }

    [Fact]
    public void Properties_Map()
    {
        var model = new Models.Client()
        {
            Properties =
            {
                {"foo1", "bar1"},
                {"foo2", "bar2"},
            }
        };


        var mappedEntity = model.ToEntity();

        mappedEntity.Properties.Count.Should().Be(2);
        var foo1 = mappedEntity.Properties.FirstOrDefault(x => x.Key == "foo1");
        foo1.Should().NotBeNull();
        foo1.Value.Should().Be("bar1");
        var foo2 = mappedEntity.Properties.FirstOrDefault(x => x.Key == "foo2");
        foo2.Should().NotBeNull();
        foo2.Value.Should().Be("bar2");



        var mappedModel = mappedEntity.ToModel();

        mappedModel.Properties.Count.Should().Be(2);
        mappedModel.Properties.ContainsKey("foo1").Should().BeTrue();
        mappedModel.Properties.ContainsKey("foo2").Should().BeTrue();
        mappedModel.Properties["foo1"].Should().Be("bar1");
        mappedModel.Properties["foo2"].Should().Be("bar2");
    }

    [Fact]
    public void duplicates_properties_in_db_map()
    {
        var entity = new Duende.IdentityServer.EntityFramework.Entities.Client
        {
            Properties = new System.Collections.Generic.List<Duende.IdentityServer.EntityFramework.Entities.ClientProperty>()
            {
                new Duende.IdentityServer.EntityFramework.Entities.ClientProperty{Key = "foo1", Value = "bar1"},
                new Duende.IdentityServer.EntityFramework.Entities.ClientProperty{Key = "foo1", Value = "bar2"},
            }
        };

        Action modelAction = () => entity.ToModel();
        modelAction.Should().Throw<Exception>();
    }

    [Fact]
    public void missing_values_should_use_defaults()
    {
        var entity = new Entities.Client
        {
            ClientSecrets = new System.Collections.Generic.List<Entities.ClientSecret>
            {
                new Entities.ClientSecret
                {
                }
            }
        };

        var def = new Models.Client
        {
            ClientSecrets = { new Models.Secret("foo") }
        };

        var model = entity.ToModel();
        model.ProtocolType.Should().Be(def.ProtocolType);
        model.ClientSecrets.First().Type.Should().Be(def.ClientSecrets.First().Type);
    }


    [Fact]
    public void mapping_model_to_entity_maps_all_properties()
    {
        var notMapped = new string[]
        {
            "Id",
            "Updated",
            "LastAccessed",
            "NonEditable",
        };

        var notAutoInitialized = new string[]
        {
            "AllowedGrantTypes",
        };

        MapperTestHelpers
            .AllPropertiesAreMapped<Models.Client, Entities.Client>(
                notAutoInitialized,
                source => {
                    source.AllowedIdentityTokenSigningAlgorithms.Add("RS256"); // We have to add values, otherwise the converter will produce null
                    source.AllowedGrantTypes = new List<string>
                    {
                        GrantType.AuthorizationCode // We need to set real values for the grant types, because they are validated
                    };
                },
                source => source.ToEntity(),
                notMapped,
                out var unmappedMembers)
            .Should()
            .BeTrue($"{string.Join(',', unmappedMembers)} should be mapped");
    }

    [Fact]
    public void mapping_entity_to_model_maps_all_properties()
    {
        MapperTestHelpers
            .AllPropertiesAreMapped<Entities.Client, Models.Client>(
                source => source.ToModel(),
                out var unmappedMembers)
            .Should()
            .BeTrue($"{string.Join(',', unmappedMembers)} should be mapped");
    }

    enum TestEnum
    {
        Foo, Bar
    }

    class ExtendedClientEntity : Entities.Client
    {
        public int Number { get; set; }
        public bool Flag { get; set; }
        public TestEnum Enumeration { get; set; }
        public IEnumerable<string> Enumerable { get; set; }
        public List<string> List { get; set; }
        public Dictionary<string, string> Dictionary { get; set; }

        public ExtendedClientEntity(Entities.Client client)
        {
            var extendedType = typeof(ExtendedClientEntity);
            var baseType = typeof(Entities.Client);

            foreach (var baseProperty in baseType.GetProperties())
            {
                var derivedProperty = extendedType.GetProperty(baseProperty.Name);
                if (derivedProperty != null && derivedProperty.CanWrite && baseProperty.CanRead)
                {
                    var value = baseProperty.GetValue(client);
                    derivedProperty.SetValue(this, value);
                }
            }
        }
    }

    class ExtendedClientModel : Models.Client
    {
        public ExtendedClientEntity ToExtendedEntity()
        {
            return new ExtendedClientEntity(this.ToEntity());
        }
    }

    private static int CountForgottenProperties<TBase, TDerived>() where TDerived : TBase
    {
        var baseProperties = typeof(TBase).GetProperties();
        var derivedProperties = typeof(TDerived).GetProperties();

        return derivedProperties
            .Count(derivedProperty => !baseProperties.Any(baseProp => baseProp.Name == derivedProperty.Name));
    }

    [Fact]
    public void forgetting_to_map_properties_is_checked_by_tests()
    {
        var notMapped = new string[]
        {
            "Id",
            "Updated",
            "LastAccessed",
            "NonEditable"
        };

        var notAutoInitialized = new string[]
        {
            "AllowedGrantTypes",
        };

        MapperTestHelpers
            .AllPropertiesAreMapped<ExtendedClientModel, ExtendedClientEntity>(
                notAutoInitialized,
                source => {
                    source.AllowedIdentityTokenSigningAlgorithms.Add("RS256"); // We have to add values, otherwise the converter will produce null
                    source.AllowedGrantTypes = new List<string>
                    {
                        GrantType.AuthorizationCode // We need to set real values for the grant types, because they are validated
                    };
                },
                source => source.ToExtendedEntity(),
                notMapped,
                out var unmappedMembers)
            .Should()
            .BeFalse();
        unmappedMembers.Count.Should().Be(CountForgottenProperties<Entities.Client, ExtendedClientEntity>());
    }
}