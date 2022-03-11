// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Extensions;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Duende.IdentityServer.EntityFramework.DbContexts;

/// <summary>
/// DbContext for the IdentityServer operational data.
/// </summary>
/// <seealso cref="Microsoft.EntityFrameworkCore.DbContext" />
/// <seealso cref="IPersistedGrantDbContext" />
public class PersistedGrantDbContext : PersistedGrantDbContext<PersistedGrantDbContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PersistedGrantDbContext"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">storeOptions</exception>
    public PersistedGrantDbContext(DbContextOptions<PersistedGrantDbContext> options)
        : base(options)
    {
    }
}

/// <summary>
/// DbContext for the IdentityServer operational data.
/// </summary>
/// <seealso cref="Microsoft.EntityFrameworkCore.DbContext" />
/// <seealso cref="IPersistedGrantDbContext" />
public class PersistedGrantDbContext<TContext> : DbContext, IPersistedGrantDbContext
    where TContext : DbContext, IPersistedGrantDbContext
{
    /// <summary>
    /// The options for this store.
    /// </summary>
    protected OperationalStoreOptions StoreOptions { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistedGrantDbContext"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">storeOptions</exception>
    public PersistedGrantDbContext(DbContextOptions options)
        : base(options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistedGrantDbContext"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="operationalStoreOptions"></param>
    /// <exception cref="ArgumentNullException">storeOptions</exception>
    public PersistedGrantDbContext(DbContextOptions options, OperationalStoreOptions operationalStoreOptions)
        : base(options)
    {
        StoreOptions = operationalStoreOptions;
    }

    /// <inheritdoc/>
    public DbSet<PersistedGrant> PersistedGrants { get; set; }

    /// <inheritdoc/>
    public DbSet<DeviceFlowCodes> DeviceFlowCodes { get; set; }

    /// <inheritdoc/>
    public DbSet<Key> Keys { get; set; }

    /// <inheritdoc/>
    public DbSet<ServerSideSession> ServerSideSessions { get; set; }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (StoreOptions is null)
        {
            StoreOptions = this.GetService<OperationalStoreOptions>();

            if (StoreOptions is null)
            {
                throw new ArgumentNullException(nameof(StoreOptions));
            }
        }
        
        modelBuilder.ConfigurePersistedGrantContext(StoreOptions);

        base.OnModelCreating(modelBuilder);
    }
}