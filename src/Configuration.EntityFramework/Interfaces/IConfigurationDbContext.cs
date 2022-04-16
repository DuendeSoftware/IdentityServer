// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Configuration.EntityFramework.Clients;
using Microsoft.EntityFrameworkCore;

namespace Duende.IdentityServer.Configuration.EntityFramework.Interfaces;

/// <summary>
/// Abstraction for the configuration context.
/// </summary>
/// <seealso cref="System.IDisposable" />
public interface IConfigurationDbContext : IDisposable
{
    /// <summary>
    /// Gets or sets the clients.
    /// </summary>
    /// <value>
    /// The clients.
    /// </value>
    DbSet<ClientEntity>? Clients { get; set; }
        
    /// <summary>
    /// Gets or sets the clients' CORS origins.
    /// </summary>
    /// <value>
    /// The clients CORS origins.
    /// </value>
    DbSet<ClientCorsOrigin>? ClientCorsOrigins { get; set; }
}