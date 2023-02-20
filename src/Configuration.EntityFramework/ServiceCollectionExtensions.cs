using Microsoft.Extensions.DependencyInjection;

namespace Duende.IdentityServer.Configuration.EntityFramework;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClientConfigurationStore(this IServiceCollection services)
    {
        return services.AddTransient<IClientConfigurationStore, ClientConfigurationStore>();
    }
}