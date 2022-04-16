using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Configuration.EntityFramework.Tests;

/// <summary>
/// Base class for integration tests, responsible for initializing test database providers & an xUnit class fixture
/// </summary>
/// <typeparam name="TClass">The type of the class.</typeparam>
/// <typeparam name="TDbContext">The type of the database context.</typeparam>
/// <typeparam name="TStoreOption">The type of the store option.</typeparam>
/// <seealso cref="DatabaseProviderFixture{T}" />
public class IntegrationTest<TClass, TDbContext, TStoreOption> : IClassFixture<DatabaseProviderFixture<TDbContext>>
    where TDbContext : DbContext
    where TStoreOption : class
{
    public static readonly TheoryData<DbContextOptions<TDbContext>> TestDatabaseProviders;
    protected static readonly TStoreOption StoreOptions = Activator.CreateInstance<TStoreOption>();

    static IntegrationTest()
    {
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.WriteLine($"Running Local Tests for {typeof(TClass).Name}");

            TestDatabaseProviders = new TheoryData<DbContextOptions<TDbContext>>
            {
                DatabaseProviderBuilder.BuildInMemory<TDbContext, TStoreOption>(typeof(TClass).Name, StoreOptions),
                //DatabaseProviderBuilder.BuildSqlite<TDbContext>(typeof(TClass).Name),
                //DatabaseProviderBuilder.BuildLocalDb<TDbContext>(typeof(TClass).Name)
            };
        }
        else
        {
            TestDatabaseProviders = new TheoryData<DbContextOptions<TDbContext>>
            {
                DatabaseProviderBuilder.BuildInMemory<TDbContext, TStoreOption>(typeof(TClass).Name, StoreOptions),
                //DatabaseProviderBuilder.BuildSqlite<TDbContext, TStoreOption>(typeof(TClass).Name, StoreOptions)
            };
            Console.WriteLine("Skipping DB integration tests on non-Windows");
        }
    }

    protected IntegrationTest(DatabaseProviderFixture<TDbContext> fixture)
    {
        fixture.Options = TestDatabaseProviders.SelectMany(x => x.Select(y => (DbContextOptions<TDbContext>) y))
            .ToList();
    }
}