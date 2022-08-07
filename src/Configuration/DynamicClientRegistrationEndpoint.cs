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
        if (context.Request.ContentType != "application/json")
        {
            context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
            return;
        }

        DynamicClientRegistrationDocument request;
        try
        {
            request = await context.Request.ReadFromJsonAsync<DynamicClientRegistrationDocument>();
        }
        catch (Exception e)
        {
            // todo: return error response
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
        

        // validate body values and construct Client object
        var result = await _validator.ValidateAsync(context.User, request);

        if (result.IsError)
        {
            // todo: return error response
            throw new InvalidOperationException(result.Error);
        }

        // todo client secret - generate here, or in validator?
        // should there be a default lifetime on the secret?
        string sharedSecret;
        if (!result.Client.ClientSecrets.Any())
        {
            // for now just generate a shared secret
            sharedSecret = CryptoRandom.CreateUniqueId();
            result.Client.ClientSecrets.Add(new Secret(sharedSecret.ToSha256()));
        }
         
        // pass body, caller identity and Client to validator
        result = await _customValidator.ValidateAsync(context.User, request, result.Client);
        
        // create client in configuration system
        await _store.AddAsync(result.Client);

        // return response
        // Upon a successful registration request, the authorization server
        // returns a client identifier for the client.  The server responds with
        // an HTTP 201 Created status code and a body of type "application/json"
        // with content as described in Section 3.2.1.
        
        //When a registration error condition occurs, the authorization server
        //returns an HTTP 400 status code (unless otherwise specified) with
        //    content type "application/json" consisting of a JSON object [RFC7159]
        // describing the error in the response body.

        //    Two members are defined for inclusion in the JSON object:

        // error
        // REQUIRED.  Single ASCII error code string.

        //     error_description
        // OPTIONAL.  Human-readable ASCII text description of the error used
        // for debugging.
        
        // todo: generate response
    }
}