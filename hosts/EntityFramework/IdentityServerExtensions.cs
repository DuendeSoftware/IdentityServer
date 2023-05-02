// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Configuration.EntityFramework;
using Duende.IdentityServer.EntityFramework.Stores;
using IdentityModel;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerHost;

internal static class IdentityServerExtensions
{
    internal static WebApplicationBuilder ConfigureIdentityServer(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("db");

        builder.Services.AddIdentityServer(options =>
        {
            options.Authentication.CoordinateClientLifetimesWithUserSession = true;
            options.ServerSideSessions.UserDisplayNameClaimType = JwtClaimTypes.Name;
            options.ServerSideSessions.RemoveExpiredSessions = true;
            options.ServerSideSessions.RemoveExpiredSessionsFrequency = TimeSpan.FromSeconds(10);
            options.ServerSideSessions.ExpiredSessionsTriggerBackchannelLogout = true;

            options.KeyManagement.KeyPath = "custom/path/to/keys";
        })
            .AddTestUsers(TestUsers.Users)
            // this adds the config data from DB (clients, resources, CORS)
            .AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = builder => builder.UseSqlServer(connectionString);
            })
            // this adds the operational data from DB (codes, tokens, consents)
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = builder => builder.UseSqlServer(connectionString);

                // this enables automatic token cleanup. this is optional.
                options.EnableTokenCleanup = false;
                options.RemoveConsumedTokens = true;
                options.TokenCleanupInterval = 10; // interval in seconds
            })
            .AddServerSideSessions()
            // this is something you will want in production to reduce load on and requests to the DB
            //.AddConfigurationStoreCache()
            ;

        var efKeyStoreDescriptor = builder.Services.FirstOrDefault(d => d.ImplementationType == typeof(SigningKeyStore));
        builder.Services.Remove(efKeyStoreDescriptor);

        builder.Services.AddIdentityServerConfiguration(opt =>
        {
                // opt.DynamicClientRegistration.SecretLifetime = TimeSpan.FromHours(1);
        })
            .AddClientConfigurationStore();

        return builder;
    }
}