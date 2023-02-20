using Duende.IdentityServer.Configuration.Configuration;
using Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Duende.IdentityServer.Configuration;

public static class ConfigurationServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityServerConfiguration(this IServiceCollection services, Action<IdentityServerConfigurationOptions> setupAction)
    {
        services.Configure(setupAction);
        return AddIdentityServerConfiguration(services);
    }

    public static IServiceCollection AddIdentityServerConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IdentityServerConfigurationOptions>(configuration);
        return services.AddIdentityServerConfiguration();
    }

    public static IServiceCollection AddIdentityServerConfiguration(this IServiceCollection services)
    {
        services.AddTransient<DynamicClientRegistrationEndpoint>();
        
        services.TryAddTransient<IDynamicClientRegistrationValidator, DefaultDynamicClientRegistrationValidator>();
        services.TryAddTransient<ICustomDynamicClientRegistrationValidator, DefaultCustomDynamicClientRegistrationValidator>();
        
        return services;
    }
}
