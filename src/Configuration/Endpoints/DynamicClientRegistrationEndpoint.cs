using System.Net.Http.Headers;
using System.Text.Json;
using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Configuration.ResponseGeneration;
using Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Configuration;

public class DynamicClientRegistrationEndpoint
{
    
    private readonly IDynamicClientRegistrationValidator _validator;
    private readonly IDynamicClientRegistrationRequestProcessor _processor;
    private readonly IDynamicClientRegistrationResponseGenerator _responseGenerator;
    private readonly ILogger<DynamicClientRegistrationEndpoint> _logger;

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

    public async Task Process(HttpContext context)
    {
        // Check content type
        if (!HasCorrectContentType(context.Request))
        {
            WriteContentTypeError(context.Response);
            return;
        }

        // Parse body
        var request = await TryParseAsync(context.Request);
        if(request == null)
        {
            await _responseGenerator.WriteBadRequestError(context);
            return;
        }

        // Validate request values 
        var result = await _validator.ValidateAsync(context.User, request);

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
        
    private void WriteContentTypeError(HttpResponse response)
    {
        _logger.LogDebug("Invalid content type in dynamic client registration request");
        response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
    }

    private async Task<DynamicClientRegistrationRequest?> TryParseAsync(HttpRequest request)
    {
        try
        {
            var document = await request.ReadFromJsonAsync<DynamicClientRegistrationRequest>();
            if(document == null) 
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
