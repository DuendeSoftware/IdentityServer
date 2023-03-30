// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Net.Http.Headers;
using System.Text.Json;
using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Configuration.RequestProcessing;
using Duende.IdentityServer.Configuration.ResponseGeneration;
using Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Configuration;

/// <summary>
/// The dynamic client registration endpoint
/// </summary>
public class DynamicClientRegistrationEndpoint
{
    private readonly IDynamicClientRegistrationValidator _validator;
    private readonly IDynamicClientRegistrationRequestProcessor _processor;
    private readonly IDynamicClientRegistrationResponseGenerator _responseGenerator;
    private readonly ILogger<DynamicClientRegistrationEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicClientRegistrationEndpoint" /> class.
    /// </summary>
    public DynamicClientRegistrationEndpoint(
        IDynamicClientRegistrationValidator validator,
        IDynamicClientRegistrationRequestProcessor processor,
        IDynamicClientRegistrationResponseGenerator responseGenerator,
        ILogger<DynamicClientRegistrationEndpoint> logger)
    {
        _validator = validator;
        _logger = logger;
        _processor = processor;
        _responseGenerator = responseGenerator;
    }

    /// <summary>
    /// Processes requests to the dynamic client registration endpoint
    /// </summary>
    public async Task Process(HttpContext context)
    {
        // Check content type
        if (!HasCorrectContentType(context.Request))
        {
            await _responseGenerator.WriteContentTypeError(context);
            return;
        }

        // Parse body
        var request = await TryParseAsync(context.Request);
        if (request == null)
        {
            await _responseGenerator.WriteBadRequestError(context);
            return;
        }

        // Validate request values 
        var result = await _validator.ValidateAsync(request, context.User);

        if (result is DynamicClientRegistrationValidationError validationError)
        {
            await _responseGenerator.WriteValidationError(context, validationError);
        }
        else if (result is DynamicClientRegistrationValidatedRequest validatedRequest)
        {
            var response = await _processor.ProcessAsync(validatedRequest);
            await _responseGenerator.WriteSuccessResponse(context, response);
        }
    }

    private static bool HasCorrectContentType(HttpRequest request)
    {
        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mt))
        {
            return false;
        }

        // Matches application/json
        if (mt.MediaType!.Equals("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private async Task<DynamicClientRegistrationRequest?> TryParseAsync(HttpRequest request)
    {
        try
        {
            var document = await request.ReadFromJsonAsync<DynamicClientRegistrationRequest>();
            if (document == null)
            {
                _logger.LogDebug("Dynamic client registration request body cannot be null");
            }
            return document;
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse dynamic client registration request body");
            return default;
        }
    }
}