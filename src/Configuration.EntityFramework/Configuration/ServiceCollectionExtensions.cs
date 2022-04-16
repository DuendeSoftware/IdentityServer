// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Configuration.EntityFramework.DbContexts;
using Duende.IdentityServer.Configuration.EntityFramework.Interfaces;
using Duende.IdentityServer.Configuration.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Duende.IdentityServer.Configuration.EntityFramework.Configuration;

/// <summary>
/// Extension methods to add EF database support to IdentityServer.
/// </summary>
public static class IdentityServerEntityFrameworkBuilderExtensions
{
    /// <summary>
    /// Add Configuration DbContext to the DI system.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="storeOptionsAction">The store options action.</param>
    /// <returns></returns>
    public static IServiceCollection AddConfigurationDbContext(this IServiceCollection services,
        Action<ConfigurationStoreOptions>? storeOptionsAction = null)
    {
        return services.AddConfigurationDbContext<ConfigurationDbContext>(storeOptionsAction);
    }

    /// <summary>
    /// Add Configuration DbContext to the DI system.
    /// </summary>
    /// <typeparam name="TContext">The IConfigurationDbContext to use.</typeparam>
    /// <param name="services"></param>
    /// <param name="storeOptionsAction">The store options action.</param>
    /// <returns></returns>
    public static IServiceCollection AddConfigurationDbContext<TContext>(this IServiceCollection services,
        Action<ConfigurationStoreOptions>? storeOptionsAction = null)
        where TContext : DbContext, IConfigurationDbContext
    {
        var options = new ConfigurationStoreOptions();
        services.AddSingleton(options);
        storeOptionsAction?.Invoke(options);

        if (options.ResolveDbContextOptions != null)
        {
            if (options.EnablePooling)
            {
                if (options.PoolSize.HasValue)
                {
                    services.AddDbContextPool<TContext>(options.ResolveDbContextOptions, options.PoolSize.Value);
                }
                else
                {
                    services.AddDbContextPool<TContext>(options.ResolveDbContextOptions);
                }
            }
            else
            {
                services.AddDbContext<TContext>(options.ResolveDbContextOptions);
            }
        }
        else
        {
            if (options.EnablePooling)
            {
                if (options.PoolSize.HasValue)
                {
                    services.AddDbContextPool<TContext>(
                        dbCtxBuilder => { options.ConfigureDbContext?.Invoke(dbCtxBuilder); }, options.PoolSize.Value);
                }
                else
                {
                    services.AddDbContextPool<TContext>(
                        dbCtxBuilder => { options.ConfigureDbContext?.Invoke(dbCtxBuilder); });
                }
            }
            else
            {
                services.AddDbContext<TContext>(dbCtxBuilder =>
                {
                    options.ConfigureDbContext?.Invoke(dbCtxBuilder);
                });
            }
        }

        services.AddScoped<IConfigurationDbContext, TContext>();

        return services;
    }
}