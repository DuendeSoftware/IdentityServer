using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;

namespace Duende.IdentityServer.Configuration;

public static class ConfigurationEndpointExtensions
{
    public static IEndpointConventionBuilder MapDynamicClientRegistration(this IEndpointRouteBuilder endpoints, string path)
    {
        return endpoints.MapPost(path, (DynamicClientRegistrationEndpoint endpoint, HttpContext context) => endpoint.Process(context));
    }
}