using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Endpoints.Results;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;
using IdentityModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Endpoints;
internal class PushedAuthorizationEndpoint : IEndpointHandler
{
    private readonly IClientSecretValidator _clientValidator;
    private readonly IAuthorizeRequestValidator _requestValidator;
    private readonly IPushedAuthorizationRequestStore _store;
    private readonly ILogger<PushedAuthorizationEndpoint> _logger;

    public PushedAuthorizationEndpoint(
        IClientSecretValidator clientValidator,
        IAuthorizeRequestValidator requestValidator,
        IPushedAuthorizationRequestStore store,
        ILogger<PushedAuthorizationEndpoint> logger)
    {
        _clientValidator = clientValidator;
        _requestValidator = requestValidator;
        _store = store;
        _logger = logger;
    }

    public async Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        using var activity = Tracing.BasicActivitySource.StartActivity(IdentityServerConstants.EndpointNames.PushedAuthorization);

        _logger.LogDebug("Start pushed authorization request");

        NameValueCollection values;

        if(HttpMethods.IsPost(context.Request.Method))
        {
           values = context.Request.Form.AsNameValueCollection();
        }
        else
        {
            return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
        }

        // Authenticate Client
        var client = await _clientValidator.ValidateAsync(context);
        if(client.IsError)
        {
            // TODO
            // Can we reuse code between this and the token endpoint?
        }

        // Validate Request
        var validationResult = await _requestValidator.ValidateAsync(values);

        if(validationResult.IsError)
        {
            // TODO
            // Would be nice if we could reuse the error response code from the authorize endpoint, I think.
        }

        // Create a reference value
        // REVIEW - Is 160 bits the right amount?
        //
        // The spec says 
        //
        //The probability of an attacker guessing generated tokens(and other
        //credentials not intended for handling by end - users) MUST be less than
        //or equal to 2 ^ (-128) and SHOULD be less than or equal to 2 ^ (-160).
        var referenceValue = CryptoRandom.CreateUniqueId(32, CryptoRandom.OutputFormat.Base64Url);
        var requestUri = $"urn:ietf:params:oauth:request_uri:{referenceValue}";
        var expiration = DateTime.UtcNow.AddSeconds(120); // TODO add new Client Configuration for this

        // Persist 
        await _store.StoreAsync(new Storage.Models.PushedAuthorizationRequest
        {
            RequestUri = requestUri,
            Expiration = expiration,
            // What do I store here? Original params? Or validation result?
        });


        // Return reference and expiration
        var response = new PushedAuthorizationResponse
        {
            RequestUri = requestUri,
            Expiration = expiration
        };

        // TODO - Logs and events here

        return new PushedAuthorizationResult(response);
    }
}
