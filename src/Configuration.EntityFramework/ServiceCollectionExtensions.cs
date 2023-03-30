// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace Duende.IdentityServer.Configuration.EntityFramework;


/// <summary>
/// Extension methods for adding the entity framework based client configuration
/// store implementation to DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the entity framework based client configuration store
    /// implementation to DI.
    /// </summary>
    public static IServiceCollection AddClientConfigurationStore(this IdentityServerConfigurationBuilder builder)
    {
        return builder.Services.AddTransient<IClientConfigurationStore, ClientConfigurationStore>();
    }
}