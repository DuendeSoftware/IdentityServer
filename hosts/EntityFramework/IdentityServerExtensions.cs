using IdentityModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerHost;

internal static class IdentityServerExtensions
{
    internal static WebApplicationBuilder ConfigureIdentityServer(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("db");

        builder.Services.AddIdentityServer(options =>
        {
            options.Authentication.UserDisplayNameClaimType = JwtClaimTypes.Name;
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
                options.RemoveExpiredUserSessions = false;
            })
            .AddServerSideSessions()
            // this is something you will want in production to reduce load on and requests to the DB
            //.AddConfigurationStoreCache()
            ;

        return builder;
    }
}