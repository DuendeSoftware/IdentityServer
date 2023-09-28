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
    private readonly IPushedAuthorizationRequestValidator _parValidator;
    private readonly IAuthorizeRequestValidator _authorizeRequestValidator;
    private readonly IPushedAuthorizationResponseGenerator _responseGenerator;
    private readonly IdentityServerOptions _options;
    private readonly IEventService _events;
    private readonly ILogger<PushedAuthorizationEndpoint> _logger;

    public PushedAuthorizationEndpoint(
        IClientSecretValidator clientValidator,
        IPushedAuthorizationRequestValidator parValidator,
        IAuthorizeRequestValidator authorizeRequestValidator,
        IPushedAuthorizationResponseGenerator responseGenerator,
        IEventService events,
        ILogger<PushedAuthorizationEndpoint> logger
        )
    {
        _clientValidator = clientValidator;
        _parValidator = parValidator;
        _authorizeRequestValidator = authorizeRequestValidator;
        _responseGenerator = responseGenerator;
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
            return await CreateErrorResultAsync(
                logMessage: "Client secret validation failed",
                request: null,
                client.Error,
                client.ErrorDescription);
        }

        // Perform validations specific to PAR
        var parValidationResult = await _parValidator.ValidateAsync(new PushedAuthorizationRequestValidationContext(values, client.Client));
        if (parValidationResult.IsError)
        {
            return await CreateErrorResultAsync(
                logMessage: "Pushed authorization validation failed",
                request: null,
                parValidationResult.Error,
                parValidationResult.ErrorDescription);
        }
       
        // Validate the content of the pushed authorization request
        var authValidationResult = await _authorizeRequestValidator.ValidateAsync(values);
        if(authValidationResult.IsError)
        {
            return await CreateErrorResultAsync(
                "Request validation failed",
                authValidationResult.ValidatedRequest,
                authValidationResult.Error,
                authValidationResult.ErrorDescription);
        }

        var response = await _responseGenerator.CreateResponseAsync(parValidationResult.ValidatedRequest);

        return response switch
        {
            PushedAuthorizationSuccess success => new PushedAuthorizationResult(success),
            PushedAuthorizationFailure fail => new PushedAuthorizationErrorResult(fail),
            _ => throw new Exception("Can't happen")
        };
    }

    private async Task<PushedAuthorizationErrorResult> CreateErrorResultAsync(
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

        await RaiseFailureEventAsync(request, error, errorDescription);

        return new PushedAuthorizationErrorResult(new PushedAuthorizationFailure
        {
            Error = error,
            ErrorDescription = errorDescription,
        });
    }

    private Task RaiseFailureEventAsync(ValidatedAuthorizeRequest request, string error, string errorDescription)
    {
        // TODO - Replace this event with a new one, or maybe don't introduce an event...
        return _events.RaiseAsync(new TokenIssuedFailureEvent(request, error, errorDescription));
    }

}
