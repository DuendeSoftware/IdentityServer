// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Configuration.EntityFramework.Tests;

/// <summary>
/// Helper methods to initialize DbContextOptions for the specified database provider and context.
/// </summary>
public class DatabaseProviderBuilder
{
    public static DbContextOptions<TDbContext> BuildInMemory<TDbContext, TStoreOptions>(string name,
        TStoreOptions storeOptions)
        where TDbContext : DbContext
        where TStoreOptions : class

    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(storeOptions);

        var builder = new DbContextOptionsBuilder<TDbContext>();
        builder.UseInMemoryDatabase(name);
        builder.UseApplicationServiceProvider(serviceCollection.BuildServiceProvider());
        return builder.Options;
    }

    public static DbContextOptions<TDbContext> BuildSqlite<TDbContext, TStoreOptions>(string name,
        TStoreOptions storeOptions)
        where TDbContext : DbContext
        where TStoreOptions : class
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(storeOptions);

        var builder = new DbContextOptionsBuilder<TDbContext>();
        builder.UseSqlite($"Filename=./Test.IdentityServer4.EntityFramework-3.1.0.{name}.db");
        builder.UseApplicationServiceProvider(serviceCollection.BuildServiceProvider());
            
        return builder.Options;
    }

    public static DbContextOptions<TDbContext> BuildLocalDb<TDbContext, TStoreOptions>(string name,
        TStoreOptions storeOptions)
        where TDbContext : DbContext
        where TStoreOptions : class
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(storeOptions);

        var builder = new DbContextOptionsBuilder<TDbContext>();
        builder.UseSqlServer(
            $@"Data Source=(LocalDb)\MSSQLLocalDB;database=Test.IdentityServer4.EntityFramework-3.1.0.{name};trusted_connection=yes;");
        builder.UseApplicationServiceProvider(serviceCollection.BuildServiceProvider());
        return builder.Options;
    }
}
