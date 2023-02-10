using System.Text.Json;
using System.Text.Json.Serialization;
using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;
using Duende.IdentityServer.Models;
using IdentityModel;
using Microsoft.AspNetCore.Http;

namespace Duende.IdentityServer.Configuration;

public class DynamicClientRegistrationEndpoint
{
    private readonly IDynamicClientRegistrationValidator _validator;
    private readonly ICustomDynamicClientRegistrationValidator _customValidator;
    private readonly IClientConfigurationStore _store;

    public DynamicClientRegistrationEndpoint(
        IDynamicClientRegistrationValidator validator,
        ICustomDynamicClientRegistrationValidator customValidator,
        IClientConfigurationStore store)
    {
        _validator = validator;
        _customValidator = customValidator;
        _store = store;
    }

    public async Task Process(HttpContext context)
    {
        var document = await ParseRequest(context);
        if(document == null) return;

        // validate body values and construct Client object
        var result = await ValidateAsync(context, document);

        if (result is DynamicClientRegistrationValidationError validationError)
        {
            await WriteValidationErrorResponse(validationError, context);
        }
        else if (result is DynamicClientRegistrationValidatedRequest validatedRequest)
        {
            await HandleValidationSuccess(validatedRequest, context);
        }
    }

    private async Task<DynamicClientRegistrationValidationResult> ValidateAsync(HttpContext context, DynamicClientRegistrationRequest request)
    {
        // validate body values and construct Client object
        var result = await _validator.ValidateAsync(context.User, request);

        if (result is DynamicClientRegistrationValidationError errorResult)
        {
            return errorResult;
        }
        else if (result is DynamicClientRegistrationValidatedRequest validatedRequest)
        {
            return await _customValidator.ValidateAsync(context.User, validatedRequest);
        } 
        else 
        {
            throw new Exception("Can't happen");
        }
    }

    private static async Task<DynamicClientRegistrationRequest?> ParseRequest(HttpContext context)
    {
        // de-serialize body
        if (!context.Request.HasJsonContentType())
        {
            context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
            return null;
        }
        
        try
        {
            var document = await context.Request.ReadFromJsonAsync<DynamicClientRegistrationResponse>()
                ?? throw new Exception("TODO");
            return document;
        }
        catch
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new DynamicClientRegistrationErrorResponse
            {
                Error = DynamicClientRegistrationErrors.InvalidClientMetadata,
                ErrorDescription = "malformed metadata document"
            });
            return null;
        }
    }

    public virtual async Task WriteValidationErrorResponse(DynamicClientRegistrationValidationError error, HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new DynamicClientRegistrationErrorResponse
        {
            Error = error.Error,
            ErrorDescription = error.ErrorDescription
        });
    }

    // TODO - Extract into a service in DI
    public virtual Task<(Secret secret, string plainText)> GenerateSecret()
    {
        var plainText = CryptoRandom.CreateUniqueId();

        // TODO should there be a default lifetime on the secret?
        var secret = new Secret(plainText.ToSha256());

        return Task.FromResult((secret, plainText));
    }


// TODO - Rename me
    public virtual async Task HandleValidationSuccess(DynamicClientRegistrationValidatedRequest validatedRequest, HttpContext context)
    {
        var secretPlainText = await AddClientSecret(validatedRequest);

        // create client in configuration system
        await _store.AddAsync(validatedRequest.Client);

// With a record type, and a with expression, there's almost nothing to do here
// Kind of no point in a response generator
// Maybe response generators aren't part of the abstraction here, now that we are changing to use ASP.NET endpoints?

        var response = (DynamicClientRegistrationResponse) validatedRequest.Original with
        {
            ClientId = validatedRequest.Client.ClientId,
            ClientSecret = secretPlainText,
            ClientSecretExpiresAt = DateTimeOffset.MaxValue.ToUnixTimeSeconds(),
        };

        await WriteResponse(context, response);
    }

    private static async Task WriteResponse(HttpContext context, DynamicClientRegistrationResponse document)
    {
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        context.Response.StatusCode = StatusCodes.Status201Created;
        await context.Response.WriteAsJsonAsync(document, options);
    }

    private async Task<string> AddClientSecret(DynamicClientRegistrationValidatedRequest validatedRequest)
    {
        if (validatedRequest.Client.ClientSecrets.Any())
        {
            // TODO - Error message
            // TODO - We still could have the validator generate a secret, but then the plaintext would need to be a property of the validation result
            throw new Exception("Validator cannot set secrets on the client because we need the plaintext of the secret outside the validator");
        }

        var (secret, plainText) = await GenerateSecret();
        validatedRequest.Client.ClientSecrets.Add(secret);
        return plainText;
    }
}