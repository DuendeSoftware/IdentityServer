using Duende.IdentityServer.Configuration.EntityFramework.Clients;
using Duende.IdentityServer.Configuration.EntityFramework.Configuration;
using Duende.IdentityServer.Configuration.EntityFramework.DbContexts;
using Duende.IdentityServer.Configuration.EntityFramework.Interfaces;
using Duende.IdentityServer.Configuration.EntityFramework.Options;
using Duende.IdentityServer.Configuration.Repositories;
using Duende.IdentityServer.Configuration.Services;
using Duende.IdentityServer.Configuration.Stores;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class IdentityServerBuilderExtensions
{
    /// <summary>
    /// Configures EF implementation of IClientStore, IResourceStore, and ICorsPolicyService with IdentityServer.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="storeOptionsAction">The store options action.</param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddEntityFrameworkConfiguration(
        this IIdentityServerBuilder builder,
        Action<ConfigurationStoreOptions>? storeOptionsAction = null)
    {
        return builder.AddEntityFrameworkConfiguration<ConfigurationDbContext>(storeOptionsAction);
    }

    /// <summary>
    /// Configures EF implementation of IClientStore, IResourceStore, and ICorsPolicyService with IdentityServer.
    /// </summary>
    /// <typeparam name="TContext">The IConfigurationDbContext to use.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="storeOptionsAction">The store options action.</param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddEntityFrameworkConfiguration<TContext>(
        this IIdentityServerBuilder builder,
        Action<ConfigurationStoreOptions>? storeOptionsAction = null)
        where TContext : DbContext, IConfigurationDbContext
    {
        builder.Services.AddConfigurationDbContext<TContext>(storeOptionsAction);
        builder.Services.AddTransient<IClientRepository, ClientRepository>();
        builder.AddClientStore<ClientStore>();
        builder.AddCorsPolicyService<CorsPolicyService>();

        return builder;
    }
}