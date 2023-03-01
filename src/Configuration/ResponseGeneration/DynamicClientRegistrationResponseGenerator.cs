using System.Text.Json;
using System.Text.Json.Serialization;
using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;
using Microsoft.AspNetCore.Http;

namespace Duende.IdentityServer.Configuration.ResponseGeneration;

public class DynamicClientRegistrationResponseGenerator : IDynamicClientRegistrationResponseGenerator
{
    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public virtual async Task WriteResponse<T>(HttpContext context, int statusCode, T response)
        where T : IDynamicClientRegistrationResponse
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(response);
    }

    public virtual async Task WriteBadRequestError(HttpContext context) =>
        await WriteResponse(context, StatusCodes.Status400BadRequest,
            new DynamicClientRegistrationErrorResponse
            {
                Error = DynamicClientRegistrationErrors.InvalidClientMetadata,
                ErrorDescription = "malformed metadata document"
            });

    public virtual async Task WriteValidationError(HttpContext context, DynamicClientRegistrationValidationError error) =>
        await WriteResponse(context, StatusCodes.Status400BadRequest,
            new DynamicClientRegistrationErrorResponse
            {
                Error = error.Error,
                ErrorDescription = error.ErrorDescription
            });

    public virtual async Task WriteSuccessResponse(HttpContext context, DynamicClientRegistrationResponse response) =>
        await WriteResponse(context, StatusCodes.Status201Created, response);
}
