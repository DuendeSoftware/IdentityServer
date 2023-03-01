using Duende.IdentityServer.Configuration.Configuration;
using Duende.IdentityServer.Configuration.ResponseGeneration;
using Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Duende.IdentityServer.Configuration;

public interface IIdentityServerConfigurationBuilder
{
    IServiceCollection Services { get; }
}

public class IdentityServerConfigurationBuilder : IIdentityServerConfigurationBuilder
{
    public IdentityServerConfigurationBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }
}

public static class ConfigurationServiceCollectionExtensions
{
    public static IIdentityServerConfigurationBuilder AddIdentityServerConfiguration(this IServiceCollection services, Action<IdentityServerConfigurationOptions> setupAction)
    {
        services.Configure(setupAction);
        return AddIdentityServerConfiguration(services);
    }

    public static IIdentityServerConfigurationBuilder AddIdentityServerConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IdentityServerConfigurationOptions>(configuration);
        return services.AddIdentityServerConfiguration();
    }

    private static IIdentityServerConfigurationBuilder AddIdentityServerConfiguration(this IServiceCollection services)
    {
        var builder = new IdentityServerConfigurationBuilder(services);

        builder.Services.AddTransient<DynamicClientRegistrationEndpoint>();
        builder.Services.AddTransient(
            resolver => resolver.GetRequiredService<IOptionsMonitor<IdentityServerConfigurationOptions>>().CurrentValue);

        builder.Services.TryAddTransient<IDynamicClientRegistrationValidator, DynamicClientRegistrationValidator>();
        builder.Services.TryAddTransient<IDynamicClientRegistrationRequestProcessor, DynamicClientRegistrationRequestProcessor>();
        builder.Services.TryAddTransient<IDynamicClientRegistrationResponseGenerator, DynamicClientRegistrationResponseGenerator>();

        return builder;
    }
}
