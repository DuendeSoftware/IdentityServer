using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Endpoints.Results;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.Logging.Models;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;
using IdentityModel;
using Microsoft.AspNetCore.DataProtection;
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
    private readonly IDataProtector _dataProtector;
    private readonly IPushedAuthorizationRequestStore _store;
    private readonly IdentityServerOptions _options;
    private readonly IEventService _events;
    private readonly ILogger<PushedAuthorizationEndpoint> _logger;

    public PushedAuthorizationEndpoint(
        IClientSecretValidator clientValidator,
        IAuthorizeRequestValidator requestValidator,
        IDataProtectionProvider dataProtectionProvider,
        IPushedAuthorizationRequestStore store,
        IdentityServerOptions options,
        IEventService events,
        ILogger<PushedAuthorizationEndpoint> logger
        )
    {
        _clientValidator = clientValidator;
        _requestValidator = requestValidator;
        _dataProtector = dataProtectionProvider.CreateProtector("PAR");
        _store = store;
        _options = options;
        _events = events;
        _logger = logger;
    }

    public async Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        using var activity = Tracing.BasicActivitySource.StartActivity(IdentityServerConstants.EndpointNames.PushedAuthorization);

        _logger.LogDebug("Start pushed authorization request");

        NameValueCollection values;
        IFormCollection form;
        if(HttpMethods.IsPost(context.Request.Method))
        {
            form = await context.Request.ReadFormAsync();
            values = form.AsNameValueCollection();
        }
        else
        {
            return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
        }

        // Authenticate Client
        var client = await _clientValidator.ValidateAsync(context);
        if(client.IsError)
        {
            return ClientValidationError(client.Error, client.ErrorDescription);
        }

        //// Reject request_uri parameter
        if (values.Get(OidcConstants.AuthorizeRequest.RequestUri).IsPresent())
        {
            return await CreateErrorResultAsync(
                "Request validation failed",
                request: null,
                error: OidcConstants.AuthorizeErrors.InvalidRequest, 
                // REVIEW - Make sure this is the correct error code
                // Not clear what the error should be.
                // PAR spec says you can't use request_uri, but doesn't say what to do about it
                // https://datatracker.ietf.org/doc/html/rfc9126#name-request
                // JAR spec has related error conditions, do we reuse those ?
                // https://www.rfc-editor.org/rfc/rfc9101#name-authorization-server-respon

                errorDescription: "Cannot use request_uri with PAR"
                );
        }

        // Validate Request
        var validationResult = await _requestValidator.ValidateAsync(values);

        if(validationResult.IsError)
        {
            return await CreateErrorResultAsync(
                "Request validation failed",
                validationResult.ValidatedRequest,
                validationResult.Error,
                validationResult.ErrorDescription);
        }

        // Create a reference value
        // REVIEW - Is 32 the right length?
        //
        // The spec says 
        //
        //The probability of an attacker guessing generated tokens(and other
        //credentials not intended for handling by end - users) MUST be less than
        //or equal to 2 ^ (-128) and SHOULD be less than or equal to 2 ^ (-160).
        var referenceValue = CryptoRandom.CreateUniqueId(32, CryptoRandom.OutputFormat.Hex);
        var requestUri = $"urn:ietf:params:oauth:request_uri:{referenceValue}";
        
        
        var expiration = client.Client.PushedAuthorizationLifetime ?? _options.PushedAuthorization.Lifetime;

        // Serialize
        var serialized = ObjectSerializer.ToString(form.ToDictionary());

        // Data Protect
        var protectedData = _dataProtector.Protect(serialized);

        // Persist 
        await _store.StoreAsync(new Storage.Models.PushedAuthorizationRequest
        {
            RequestUri = requestUri,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiration),
            Parameters = protectedData
        });


        // Return reference and expiration
        var response = new PushedAuthorizationSuccess
        {
            RequestUri = requestUri,
            ExpiresIn = expiration
        };

        // TODO - Logs and events here?

        return new PushedAuthorizationResult(response);
    }

    private PushedAuthorizationResult ClientValidationError(string error, string errorDescription = null)
    {
        var response = new PushedAuthorizationFailure
        {
            Error = error,
            ErrorDescription = errorDescription,
        };

        return new PushedAuthorizationResult(response);
    }

    // TODO - Copied from AuthorizeEndpointBase
    private async Task<PushedAuthorizationResult> CreateErrorResultAsync(
        string logMessage,
        ValidatedAuthorizeRequest request = null,
        string error = OidcConstants.AuthorizeErrors.ServerError,
        string errorDescription = null,
        bool logError = true)
    {
        if (logError)
        {
            _logger.LogError(logMessage);
        }

        if (request != null)
        {
            var details = new AuthorizeRequestValidationLog(request, _options.Logging.AuthorizeRequestSensitiveValuesFilter);
            _logger.LogInformation("{@validationDetails}", details);
        }

        // TODO: should we raise a failure event for all errors to the authorize endpoint?
        await RaiseFailureEventAsync(request, error, errorDescription);

        return new PushedAuthorizationResult(new PushedAuthorizationFailure
        {
            Error = error,
            ErrorDescription = errorDescription,
        });
    }

    private Task RaiseFailureEventAsync(ValidatedAuthorizeRequest request, string error, string errorDescription)
    {
        return _events.RaiseAsync(new TokenIssuedFailureEvent(request, error, errorDescription));
    }

}
