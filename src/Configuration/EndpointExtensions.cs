using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Duende.IdentityServer.Configuration;

public static class EndpointExtensions
{
    public static IEndpointConventionBuilder MapDynamicClientRegistration(this IEndpointRouteBuilder endpoints, string path)
    {
        var endpoint = endpoints.ServiceProvider.GetRequiredService<DynamicClientRegistrationEndpoint>();

        return endpoints.MapPost(path, endpoint.Process);
    }
}