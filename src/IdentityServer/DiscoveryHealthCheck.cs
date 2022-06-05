using System;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Duende.IdentityServer;

public class DiscoveryHealthCheck : IHealthCheck
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DiscoveryHealthCheck(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var handler = httpContext.RequestServices.GetRequiredService<DiscoveryEndpoint>();

        try
        {
            var result = await handler.ProcessAsync(httpContext);

            return HealthCheckResult.Healthy();
        }
        catch (Exception e)
        {
            return new HealthCheckResult(context.Registration.FailureStatus);
        }
    }
}