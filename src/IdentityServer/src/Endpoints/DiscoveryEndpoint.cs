// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Endpoints.Results;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Endpoints
{
    internal class DiscoveryEndpoint : IEndpointHandler
    {
        private readonly ILogger _logger;

        private readonly IdentityServerOptions _options;

        private readonly IDiscoveryResponseGenerator _responseGenerator;

        public DiscoveryEndpoint(
            IdentityServerOptions options,
            IDiscoveryResponseGenerator responseGenerator,
            ILogger<DiscoveryEndpoint> logger)
        {
            _logger = logger;
            _options = options;
            _responseGenerator = responseGenerator;
        }

        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            _logger.LogTrace("Processing discovery request.");

            // validate HTTP
            if (!HttpMethods.IsGet(context.Request.Method))
            {
                _logger.LogWarning("Discovery endpoint only supports GET requests");
                return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
            }

            _logger.LogDebug("Start discovery request");

            if (!_options.Endpoints.EnableDiscoveryEndpoint)
            {
                _logger.LogInformation("Discovery endpoint disabled. 404.");
                return new StatusCodeResult(HttpStatusCode.NotFound);
            }

            var baseUrl = context.GetIdentityServerBaseUrl().EnsureTrailingSlash();
            var issuerUri = context.GetIdentityServerIssuerUri();

            // generate response
            _logger.LogTrace("Calling into discovery response generator: {type}", _responseGenerator.GetType().FullName);
            var response = await _responseGenerator.CreateDiscoveryDocumentAsync(baseUrl, issuerUri);

            return new DiscoveryDocumentResult(response, _options.Discovery.ResponseCacheInterval);
        }
    }
}