// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Endpoints.Results;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Endpoints;

internal class DiscoveryEndpoint : IEndpointHandler
{
    private readonly ILogger _logger;

    private readonly IdentityServerOptions _options;
    private readonly IIssuerNameService _issuerNameService;
    private readonly IServerUrls _urls;
    private readonly IDiscoveryResponseGenerator _responseGenerator;

    public DiscoveryEndpoint(
        IdentityServerOptions options,
        IIssuerNameService issuerNameService,
        IDiscoveryResponseGenerator responseGenerator,
        IServerUrls urls,
        ILogger<DiscoveryEndpoint> logger)
    {
        _logger = logger;
        _options = options;
        _issuerNameService = issuerNameService;
        _urls = urls;
        _responseGenerator = responseGenerator;
    }

    public async Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        using var activity = Tracing.BasicActivitySource.StartActivity(Constants.EndpointNames.Discovery + "Endpoint");
        
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

        var baseUrl = _urls.BaseUrl;
        var issuerUri = await _issuerNameService.GetCurrentAsync();

        // generate response
        _logger.LogTrace("Calling into discovery response generator: {type}", _responseGenerator.GetType().FullName);
        var response = await _responseGenerator.CreateDiscoveryDocumentAsync(baseUrl, issuerUri);

        return new DiscoveryDocumentResult(response, _options.Discovery.ResponseCacheInterval);
    }
}