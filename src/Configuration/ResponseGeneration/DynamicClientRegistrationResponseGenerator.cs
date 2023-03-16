// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Configuration.ResponseGeneration;

/// <inheritdoc/>
public class DynamicClientRegistrationResponseGenerator : IDynamicClientRegistrationResponseGenerator
{
    /// <summary>
    /// The options used for serializing json in responses.
    /// </summary>
    protected JsonSerializerOptions SerializerOptions { get; set; } = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ILogger<DynamicClientRegistrationResponseGenerator> _logger;

    /// <inheritdoc/>
    public DynamicClientRegistrationResponseGenerator(ILogger<DynamicClientRegistrationResponseGenerator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public virtual async Task WriteResponse<T>(HttpContext context, int statusCode, T response)
        where T : IDynamicClientRegistrationResponse
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(response, SerializerOptions);
    }

    /// <inheritdoc/>
    public virtual Task WriteContentTypeError(HttpContext context)
    {
        _logger.LogDebug("Invalid content type in dynamic client registration request");
        context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual async Task WriteBadRequestError(HttpContext context) =>
        await WriteResponse(context, StatusCodes.Status400BadRequest,
            new DynamicClientRegistrationErrorResponse
            {
                Error = DynamicClientRegistrationErrors.InvalidClientMetadata,
                ErrorDescription = "malformed metadata document"
            });

    /// <inheritdoc/>
    public virtual async Task WriteValidationError(HttpContext context, DynamicClientRegistrationValidationError error) =>
        await WriteResponse(context, StatusCodes.Status400BadRequest,
            new DynamicClientRegistrationErrorResponse
            {
                Error = error.Error,
                ErrorDescription = error.ErrorDescription
            });

    /// <inheritdoc/>
    public virtual async Task WriteSuccessResponse(HttpContext context, DynamicClientRegistrationResponse response) =>
        await WriteResponse(context, StatusCodes.Status201Created, response);
}
