// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Configuration.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Duende.IdentityServer.Services;
using Microsoft.Extensions.DependencyInjection;
using Duende.IdentityServer.Extensions;

namespace Duende.IdentityServer.Hosting
{
    internal class CorsPolicyProvider : ICorsPolicyProvider
    {
        private readonly ILogger _logger;
        private readonly ICorsPolicyProvider _inner;
        private readonly IdentityServerOptions _options;
        private readonly IHttpContextAccessor _httpContext;

        public CorsPolicyProvider(
            ILogger<CorsPolicyProvider> logger,
            Decorator<ICorsPolicyProvider> inner,
            IdentityServerOptions options,
            IHttpContextAccessor httpContext)
        {
            _logger = logger;
            _inner = inner.Instance;
            _options = options;
            _httpContext = httpContext;
        }

        public Task<CorsPolicy> GetPolicyAsync(HttpContext context, string policyName)
        {
            if (_options.Cors.CorsPolicyName == policyName)
            {
                return ProcessAsync(context);
            }
            else
            {
                return _inner.GetPolicyAsync(context, policyName);
            }
        }

        private async Task<CorsPolicy> ProcessAsync(HttpContext context)
        {
            var origin = context.Request.GetCorsOrigin();
            if (origin != null)
            {
                var path = context.Request.Path;
                if (IsPathAllowed(path))
                {
                    _logger.LogDebug("CORS request made for path: {path} from origin: {origin}", path, origin);

                    // manually resolving this from DI because this: 
                    // https://github.com/aspnet/CORS/issues/105
                    var corsPolicyService = _httpContext.HttpContext.RequestServices.GetRequiredService<ICorsPolicyService>();

                    if (await corsPolicyService.IsOriginAllowedAsync(origin))
                    {
                        _logger.LogDebug("CorsPolicyService allowed origin: {origin}", origin);
                        return Allow(origin);
                    }
                    else
                    {
                        _logger.LogWarning("CorsPolicyService did not allow origin: {origin}", origin);
                    }
                }
                else
                {
                    _logger.LogDebug("CORS request made for path: {path} from origin: {origin} but was ignored because path was not for an allowed IdentityServer CORS endpoint", path, origin);
                }
            }

            return null;
        }

        private CorsPolicy Allow(string origin)
        {
            var policyBuilder = new CorsPolicyBuilder()
                .WithOrigins(origin)
                .AllowAnyHeader()
                .AllowAnyMethod();

            if (_options.Cors.PreflightCacheDuration.HasValue)
            {
                policyBuilder.SetPreflightMaxAge(_options.Cors.PreflightCacheDuration.Value);
            }

            return policyBuilder.Build();
        }

        private bool IsPathAllowed(PathString path)
        {
            return _options.Cors.CorsPaths.Any(x => path == x);
        }
    }
}
