// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.EntityFramework.Options;
using Duende.IdentityServer.EntityFramework.Stores;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IntegrationTests.Stores;

public class IdentityProviderStoreTests : IntegrationTest<IdentityProviderStoreTests, ConfigurationDbContext, ConfigurationStoreOptions>
{
    public IdentityProviderStoreTests(DatabaseProviderFixture<ConfigurationDbContext> fixture) : base(fixture)
    {
        foreach (var options in TestDatabaseProviders.SelectMany(x => x.Select(y => (DbContextOptions<ConfigurationDbContext>)y)).ToList())
        {
            using (var context = new ConfigurationDbContext(options))
                context.Database.EnsureCreated();
        }
    }



    [Theory, MemberData(nameof(TestDatabaseProviders))]
    public async Task GetBySchemeAsync_should_find_by_scheme(DbContextOptions<ConfigurationDbContext> options)
    {
        using (var context = new ConfigurationDbContext(options))
        {
            var idp = new OidcProvider
            {
                Scheme = "scheme1", Type = "oidc"
            };
            context.IdentityProviders.Add(idp.ToEntity());
            context.SaveChanges();
        }

        using (var context = new ConfigurationDbContext(options))
        {
            var store = new IdentityProviderStore(context, FakeLogger<IdentityProviderStore>.Create(), new NoneCancellationTokenProvider());
            var item = await store.GetBySchemeAsync("scheme1");

            item.Should().NotBeNull();
        }
    }


    [Theory, MemberData(nameof(TestDatabaseProviders))]
    public async Task GetBySchemeAsync_should_filter_by_type(DbContextOptions<ConfigurationDbContext> options)
    {
        using (var context = new ConfigurationDbContext(options))
        {
            var idp = new OidcProvider
            {
                Scheme = "scheme2", Type = "saml"
            };
            context.IdentityProviders.Add(idp.ToEntity());
            context.SaveChanges();
        }

        using (var context = new ConfigurationDbContext(options))
        {
            var store = new IdentityProviderStore(context, FakeLogger<IdentityProviderStore>.Create(), new NoneCancellationTokenProvider());
            var item = await store.GetBySchemeAsync("scheme2");

            item.Should().BeNull();
        }
    }


    [Theory, MemberData(nameof(TestDatabaseProviders))]
    public async Task GetBySchemeAsync_should_filter_by_scheme_casing(DbContextOptions<ConfigurationDbContext> options)
    {
        using (var context = new ConfigurationDbContext(options))
        {
            var idp = new OidcProvider
            {
                Scheme = "SCHEME3", Type = "oidc"
            };
            context.IdentityProviders.Add(idp.ToEntity());
            context.SaveChanges();
        }

        using (var context = new ConfigurationDbContext(options))
        {
            var store = new IdentityProviderStore(context, FakeLogger<IdentityProviderStore>.Create(), new NoneCancellationTokenProvider());
            var item = await store.GetBySchemeAsync("scheme3");

            item.Should().BeNull();
        }
    }
}