using IdentityModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace DPoPApi;

static class DPoPServiceCollectionExtensions
{
    public static IServiceCollection RequireDPoPTokensForScheme(this IServiceCollection services, string scheme)
    {
        services.AddTransient<DPoPJwtBearerEvents>();
        services.AddTransient<DPoPProofValidator>();
        services.AddDistributedMemoryCache();
        services.AddTransient<IReplayCache, DefaultReplayCache>();

        services.Configure<JwtBearerOptions>(scheme, options =>
        {
            options.Challenge = OidcConstants.AuthenticationSchemes.AuthorizationHeaderDPoP;
            options.EventsType = typeof(DPoPJwtBearerEvents);
        });

        return services;
    }
    
    public static IServiceCollection PreventDPoPTokensForScheme(this IServiceCollection services, string scheme)
    {
        services.AddTransient<RequireCnfJwtBearerEvents>();

        services.Configure<JwtBearerOptions>(scheme, options =>
        {
            options.EventsType = typeof(RequireCnfJwtBearerEvents);
        });

        return services;
    }
}
