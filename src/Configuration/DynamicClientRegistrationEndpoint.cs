using System.Text.Json;
using System.Text.Json.Serialization;
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
        // de-serialize body
        if (!context.Request.HasJsonContentType())
        {
            context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
            return;
        }

        DynamicClientRegistrationDocument document;
        try
        {
            document = await context.Request.ReadFromJsonAsync<DynamicClientRegistrationDocument>();
        }
        catch (Exception e)
        {
            // todo: return error response
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new DynamicClientRegistrationErrorResponse
            {
                Error = DynamicClientRegistrationErrors.InvalidClientMetadata,
                ErrorDescription = "malformed metadata document"
            });
            
            return;
        }
        

        // validate body values and construct Client object
        var result = await _validator.ValidateAsync(context.User, document);

        if (result.IsError)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new DynamicClientRegistrationErrorResponse
            {
                Error = result.Error,
                ErrorDescription = result.ErrorDescription
            });
        }

        // todo client secret - generate here, or in validator?
        // should there be a default lifetime on the secret?
        string sharedSecret = null;
        if (!result.Client.ClientSecrets.Any())
        {
            // for now just generate a shared secret
            sharedSecret = CryptoRandom.CreateUniqueId();
            result.Client.ClientSecrets.Add(new Secret(sharedSecret.ToSha256()));
        }
         
        // pass body, caller identity and Client to validator
        result = await _customValidator.ValidateAsync(context.User, document, result.Client);
        
        if (result.IsError)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new DynamicClientRegistrationErrorResponse
            {
                Error = result.Error,
                ErrorDescription = result.ErrorDescription
            });
        }
        
        // create client in configuration system
        await _store.AddAsync(result.Client);

        document.ClientId = result.Client.ClientId;
        if (sharedSecret != null)
        {
            document.ClientSecret = sharedSecret;
            document.ClientSecretExpiresAt = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        }

        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        
        context.Response.StatusCode = StatusCodes.Status201Created;
        await context.Response.WriteAsJsonAsync(document, options);
    }
}