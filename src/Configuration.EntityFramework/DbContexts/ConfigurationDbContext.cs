// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Configuration.EntityFramework.Clients;
using Duende.IdentityServer.Configuration.EntityFramework.Extensions;
using Duende.IdentityServer.Configuration.EntityFramework.Interfaces;
using Duende.IdentityServer.Configuration.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Configuration.EntityFramework.DbContexts;

public class ConfigurationDbContext : ConfigurationDbContext<ConfigurationDbContext>
{
    public ConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options)
        : base(options)
    {
    }
}

public class ConfigurationDbContext<TContext> : DbContext, IConfigurationDbContext
    where TContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationDbContext"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">storeOptions</exception>
    public ConfigurationDbContext(DbContextOptions<TContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the clients.
    /// </summary>
    /// <value>
    /// The clients.
    /// </value>
    public DbSet<ClientEntity>? Clients { get; set; }

    /// <summary>
    /// Gets or sets the clients' CORS origins.
    /// </summary>
    /// <value>
    /// The clients CORS origins.
    /// </value>
    public DbSet<ClientCorsOrigin>? ClientCorsOrigins { get; set; }

    /// <summary>
    /// Override this method to further configure the model that was discovered by convention from the entity types
    /// exposed in <see cref="T:Microsoft.EntityFrameworkCore.DbSet`1" /> properties on your derived context. The resulting model may be cached
    /// and re-used for subsequent instances of your derived context.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context. Databases (and other extensions) typically
    /// define extension methods on this object that allow you to configure aspects of the model that are specific
    /// to a given database.</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <remarks>
    /// If a model is explicitly set on the options for this context (via <see cref="M:Microsoft.EntityFrameworkCore.DbContextOptionsBuilder.UseModel(Microsoft.EntityFrameworkCore.Metadata.IModel)" />)
    /// then this method will not be run.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var storeOptions = this.GetService<ConfigurationStoreOptions>();

        if (storeOptions is null)
        {
            throw new ArgumentNullException(nameof(storeOptions));
        }
            
        modelBuilder.ConfigureClientContext(storeOptions);
        /*modelBuilder.ConfigureResourcesContext(storeOptions);
        modelBuilder.ConfigureIdentityProviderContext(storeOptions);*/

        base.OnModelCreating(modelBuilder);
    }

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        if (!optionsBuilder.Options.IsFrozen)
        {
            optionsBuilder.ConfigureWarnings(w => w.Ignore(new EventId[] { RelationalEventId.MultipleCollectionIncludeWarning }));
        }
    }
}