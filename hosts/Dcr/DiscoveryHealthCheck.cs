using Duende.IdentityServer.Endpoints.Results;
using Duende.IdentityServer.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Duende.IdentityServer;

public class DiscoveryHealthCheck : IHealthCheck
{
    private readonly IEnumerable<Hosting.Endpoint> _endpoints;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DiscoveryHealthCheck(IEnumerable<Hosting.Endpoint> endpoints, IHttpContextAccessor httpContextAccessor)
    {
        _endpoints = endpoints;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = _endpoints.FirstOrDefault(x => x.Name == IdentityServerConstants.EndpointNames.Discovery);
            if (endpoint != null)
            {
                var handler = _httpContextAccessor.HttpContext.RequestServices.GetRequiredService(endpoint.Handler) as IEndpointHandler;
                if (handler != null)
                {
                    var result = await handler.ProcessAsync(_httpContextAccessor.HttpContext);
                    if (result is DiscoveryDocumentResult)
                    {
                        return HealthCheckResult.Healthy();
                    }
                }
            }
        }
        catch
        {
        }
        
        return new HealthCheckResult(context.Registration.FailureStatus);
    }
}

public class DiscoveryKeysHealthCheck : IHealthCheck
{
    private readonly IEnumerable<Hosting.Endpoint> _endpoints;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DiscoveryKeysHealthCheck(IEnumerable<Hosting.Endpoint> endpoints, IHttpContextAccessor httpContextAccessor)
    {
        _endpoints = endpoints;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = _endpoints.FirstOrDefault(x => x.Name == IdentityServerConstants.EndpointNames.Jwks);
            if (endpoint != null)
            {
                var handler = _httpContextAccessor.HttpContext.RequestServices.GetRequiredService(endpoint.Handler) as IEndpointHandler;
                if (handler != null)
                {
                    var result = await handler.ProcessAsync(_httpContextAccessor.HttpContext);
                    if (result is JsonWebKeysResult)
                    {
                        return HealthCheckResult.Healthy();
                    }
                }
            }
        }
        catch
        {
        }

        return new HealthCheckResult(context.Registration.FailureStatus);
    }
}
