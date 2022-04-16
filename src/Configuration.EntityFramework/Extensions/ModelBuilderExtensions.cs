// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Configuration.EntityFramework.Clients;
using Duende.IdentityServer.Configuration.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Duende.IdentityServer.Configuration.EntityFramework.Extensions;

/// <summary>
/// Extension methods to define the database schema for the configuration and operational data stores.
/// </summary>
public static class ModelBuilderExtensions
{
    private static EntityTypeBuilder<TEntity> ToTable<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, TableConfiguration configuration)
        where TEntity : class
    {
        return string.IsNullOrWhiteSpace(configuration.Schema) ? entityTypeBuilder.ToTable(configuration.Name) : entityTypeBuilder.ToTable(configuration.Name, configuration.Schema);
    }

    /// <summary>
    /// Configures the client context.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="storeOptions">The store options.</param>
    public static void ConfigureClientContext(this ModelBuilder modelBuilder, ConfigurationStoreOptions storeOptions)
    {
        if (!string.IsNullOrWhiteSpace(storeOptions.DefaultSchema)) modelBuilder.HasDefaultSchema(storeOptions.DefaultSchema);

        modelBuilder.Entity<ClientEntity>(client =>
        {
            client.ToTable(storeOptions.Client);
            client.HasKey(x => x.ClientId);

            client.Property(x => x.ClientId).HasMaxLength(200).IsRequired();
            client.Property(x => x.Json).HasColumnType("text").IsRequired(); //TODO we don't enforce field constrains (e.g. length) here any more.

            client.HasMany(x => x.AllowedCorsOrigins).WithOne(x => x.Client).HasForeignKey(x => x.ClientId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClientCorsOrigin>(corsOrigin =>
        {
            corsOrigin.ToTable(storeOptions.ClientCorsOrigin);
            corsOrigin.Property(x => x.Origin).HasMaxLength(150).IsRequired();
            corsOrigin.HasIndex(x => new { x.ClientId, x.Origin }).IsUnique();
        });
    }
}