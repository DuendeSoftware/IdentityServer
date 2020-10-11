// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Linq;
using Duende.IdentityServer.EntityFramework.Mappers;
using FluentAssertions;
using Xunit;
using Client = Duende.IdentityServer.Models.Client;

namespace UnitTests.Mappers
{
    public class ClientMappersTests
    {
        [Fact]
        public void AutomapperConfigurationIsValid()
        {
            ClientMappers.Mapper.ConfigurationProvider.AssertConfigurationIsValid<ClientMapperProfile>();
        }

        [Fact]
        public void Can_Map()
        {
            var model = new Client();
            var mappedEntity = model.ToEntity();
            var mappedModel = mappedEntity.ToModel();

            Assert.NotNull(mappedModel);
            Assert.NotNull(mappedEntity);
        }

        [Fact]
        public void Properties_Map()
        {
            var model = new Client()
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
            var entity = new Duende.IdentityServer.EntityFramework.Entities.Client
            {
                ClientSecrets = new System.Collections.Generic.List<Duende.IdentityServer.EntityFramework.Entities.ClientSecret>
                {
                    new Duende.IdentityServer.EntityFramework.Entities.ClientSecret
                    {
                    }
                }
            };

            var def = new Client
            {
                ClientSecrets = { new Duende.IdentityServer.Models.Secret("foo") }
            };

            var model = entity.ToModel();
            model.ProtocolType.Should().Be(def.ProtocolType);
            model.ClientSecrets.First().Type.Should().Be(def.ClientSecrets.First().Type);
        }
    }
}