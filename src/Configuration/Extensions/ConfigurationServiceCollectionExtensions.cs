using Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Duende.IdentityServer.Configuration;

public static class ConfigurationServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityServerConfiguration(this IServiceCollection services)
    {
        services.AddTransient<DynamicClientRegistrationEndpoint>();
        
        services.TryAddTransient<IDynamicClientRegistrationValidator, DefaultDynamicClientRegistrationValidator>();
        services.TryAddTransient<ICustomDynamicClientRegistrationValidator, DefaultCustomDynamicClientRegistrationValidator>();
        
        // todo: remove later
        services.AddTransient<IClientConfigurationStore, DummyClientConfigurationStore>();
        
        return services;
    }
}