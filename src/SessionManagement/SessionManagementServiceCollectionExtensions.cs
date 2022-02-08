// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.SessionManagement;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding session management
/// </summary>
public static class SessionManagementServiceCollectionExtensions
{
    /// <summary>
    /// Adds a server-side session store using the provided store type
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddServerSideSessions<T>(this IServiceCollection services)
        where T : class, IUserSessionStore
    {
        return services
            .AddServerSideSessionStore<T>()
            .AddServerSideSessions();
    }

    /// <summary>
    /// Adds a server-side session store using the in-memory store
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddServerSideSessions(this IServiceCollection services)
    {
        services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, PostConfigureApplicationCookieTicketStore>();
        services.AddTransient<IServerTicketStore, ServerSideTicketStore>();

        // only add if not already in DI
        services.TryAddSingleton<IUserSessionStore, InMemoryUserSessionStore>();

        return services;
    }

    /// <summary>
    /// Adds a server-side session store using the supplied session store implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddServerSideSessionStore<T>(this IServiceCollection services)
        where T : class, IUserSessionStore
    {
        return services.AddTransient<IUserSessionStore, T>();
    }
}