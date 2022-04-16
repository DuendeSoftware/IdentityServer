using AutoFixture;
using Duende.IdentityServer.Configuration.EntityFramework.Clients;
using Duende.IdentityServer.Configuration.EntityFramework.DbContexts;
using Duende.IdentityServer.Configuration.EntityFramework.Options;
using Duende.IdentityServer.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Configuration.EntityFramework.Tests;

public class ClientRepositoryTests : IntegrationTest<ClientRepositoryTests, ConfigurationDbContext, ConfigurationStoreOptions>
{
    public ClientRepositoryTests(DatabaseProviderFixture<ConfigurationDbContext> fixture) : base(fixture)
    {
        var dbContextOptions = TestDatabaseProviders
            .SelectMany(x => x.Select(y => (DbContextOptions<ConfigurationDbContext>) y))
            .ToList();

        foreach (var options in dbContextOptions)
        {
            using var context = new ConfigurationDbContext(options);
            context.Database.EnsureCreated();
        }
    }

    [Theory, MemberData(nameof(TestDatabaseProviders))]
    public async Task CanAddAndReadClient(DbContextOptions<ConfigurationDbContext> options)
    {
        await using var context = new ConfigurationDbContext(options);
        var clientRepository = new ClientRepository(context, NullLogger<ClientRepository>.Instance);
        var fixture = new Fixture();
        var client = fixture.Create<Client>();
        await clientRepository.Add(client);

        var loadedClient = await clientRepository.Read(client.ClientId);

        loadedClient.Should().NotBeNull();
        loadedClient.Should().BeEquivalentTo(client);
    }

    [Theory, MemberData(nameof(TestDatabaseProviders))]
    public async Task WhenCorsOriginAllowedThenShouldExist(DbContextOptions<ConfigurationDbContext> options)
    {
        await using var context = new ConfigurationDbContext(options);
        var clientRepository = new ClientRepository(context, NullLogger<ClientRepository>.Instance);
        var client = new Client
        {
            ClientId = "client",
            AllowedCorsOrigins = new[] { "https://example.com" }
        };
        await clientRepository.Add(client);

        var corsOriginExists = await clientRepository.CorsOriginExists("https://example.com");

        corsOriginExists.Should().BeTrue();
    }

    [Theory, MemberData(nameof(TestDatabaseProviders))]
    public async Task WhenCorsOriginNotSpecifiedThenShouldNotExist(DbContextOptions<ConfigurationDbContext> options)
    {
        await using var context = new ConfigurationDbContext(options);
        var clientRepository = new ClientRepository(context, NullLogger<ClientRepository>.Instance);
        var client = new Client
        {
            ClientId = "client2",
            AllowedCorsOrigins = new[] { "https://example.com" }
        };
        await clientRepository.Add(client);

        var corsOriginExists = await clientRepository.CorsOriginExists("https://foo.com");

        corsOriginExists.Should().BeFalse();
    }
}