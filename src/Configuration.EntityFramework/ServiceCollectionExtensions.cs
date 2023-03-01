using Microsoft.Extensions.DependencyInjection;

namespace Duende.IdentityServer.Configuration.EntityFramework;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClientConfigurationStore(this IIdentityServerConfigurationBuilder builder)
    {
        return builder.Services.AddTransient<IClientConfigurationStore, ClientConfigurationStore>();
    }
}