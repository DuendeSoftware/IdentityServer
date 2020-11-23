// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.EntityFrameworkCore;

namespace IntegrationTests
{
    /// <summary>
    /// Helper methods to initialize DbContextOptions for the specified database provider and context.
    /// </summary>
    public class DatabaseProviderBuilder
    {
        public static DbContextOptions<T> BuildInMemory<T>(string name) where T : DbContext
        {
            var builder = new DbContextOptionsBuilder<T>();
            builder.UseInMemoryDatabase(name);
            return builder.Options;
        }

        public static DbContextOptions<T> BuildSqlite<T>(string name) where T : DbContext
        {
            var builder = new DbContextOptionsBuilder<T>();
            builder.UseSqlite($"Filename=./Test.IdentityServer4.EntityFramework-3.1.0.{name}.db");
            return builder.Options;
        }

        public static DbContextOptions<T> BuildLocalDb<T>(string name) where T : DbContext
        {
            var builder = new DbContextOptionsBuilder<T>();
            builder.UseSqlServer(
                $@"Data Source=(LocalDb)\MSSQLLocalDB;database=Test.IdentityServer4.EntityFramework-3.1.0.{name};trusted_connection=yes;");
            return builder.Options;
        }
    }
}