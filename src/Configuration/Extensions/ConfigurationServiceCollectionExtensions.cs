using Duende.IdentityServer.Configuration.Configuration;
using Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;
using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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

    private static IServiceCollection AddIdentityServerConfiguration(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddSingleton(resolver =>
        {
            var options = resolver.GetRequiredService<IOptions<IdentityServerConfigurationOptions>>().Value;
            var httpClientFactory = resolver.GetRequiredService<IHttpClientFactory>();
            var http = httpClientFactory.CreateClient("discovery"); // TODO - rename or make more discoverable
            return new DiscoveryCache(options.Authority, () => http);
        });
        services.AddTransient<DynamicClientRegistrationEndpoint>();
        
        services.TryAddTransient<IDynamicClientRegistrationValidator, DefaultDynamicClientRegistrationValidator>();
        services.TryAddTransient<ICustomDynamicClientRegistrationValidator, DefaultCustomDynamicClientRegistrationValidator>();
        
        // Review: Should we do this, similar to core identity server?
        // services.AddSingleton(
        //     resolver => resolver.GetRequiredService<IOptions<IdentityServerConfigurationOptions>>().Value);

        return services;
    }
}
