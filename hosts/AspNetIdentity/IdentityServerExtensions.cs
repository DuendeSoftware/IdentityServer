using IdentityServerHost.Configuration;
using IdentityServerHost.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerHost;

internal static class IdentityServerExtensions
{
    internal static WebApplicationBuilder ConfigureIdentityServer(this WebApplicationBuilder builder)
    {
        builder.Services.AddIdentityServer()
            .AddInMemoryIdentityResources(Resources.IdentityResources)
            .AddInMemoryApiResources(Resources.ApiResources)
            .AddInMemoryApiScopes(Resources.ApiScopes)
            .AddInMemoryClients(Clients.Get())
            .AddAspNetIdentity<ApplicationUser>();

        return builder;
    }
}