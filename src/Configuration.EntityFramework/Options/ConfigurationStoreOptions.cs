// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.EntityFrameworkCore;

namespace Duende.IdentityServer.Configuration.EntityFramework.Options;

/// <summary>
/// Options for configuring the configuration context.
/// </summary>
public class ConfigurationStoreOptions
{
    /// <summary>
    /// Callback to configure the EF DbContext.
    /// </summary>
    /// <value>
    /// The configure database context.
    /// </value>
    public Action<DbContextOptionsBuilder>? ConfigureDbContext { get; set; }

    /// <summary>
    /// Callback in DI resolve the EF DbContextOptions. If set, ConfigureDbContext will not be used.
    /// </summary>
    /// <value>
    /// The configure database context.
    /// </value>
    public Action<IServiceProvider, DbContextOptionsBuilder>? ResolveDbContextOptions { get; set; }

    /// <summary>
    /// Gets or sets the default schema.
    /// </summary>
    /// <value>
    /// The default schema.
    /// </value>
    public string DefaultSchema { get; set; } = null;

    /// <summary>
    /// Gets or sets the client table configuration.
    /// </summary>
    /// <value>
    /// The client.
    /// </value>
    public TableConfiguration Client { get; set; } = new("Clients");

    /// <summary>
    /// Gets or sets the client cors origin table configuration.
    /// </summary>
    /// <value>
    /// The client cors origin.
    /// </value>
    public TableConfiguration ClientCorsOrigin { get; set; } = new("ClientCorsOrigins");

    /// <summary>
    /// Gets or set if EF DbContext pooling is enabled.
    /// </summary>
    public bool EnablePooling { get; set; } = false;

    /// <summary>
    /// Gets or set the pool size to use when DbContext pooling is enabled. If not set, the EF default is used.
    /// </summary>
    public int? PoolSize { get; set; }
}