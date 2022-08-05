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
        var request = await context.Request.ReadFromJsonAsync<DynamicClientRegistrationDocument>();

        // validate body values and construct Client object
        var result = await _validator.ValidateAsync(context.User, request);
        
        // generate client_id and secet (if no jwks is included)
        // todo
        // here or in above validator?
        
        // pass body, caller identity and Client to validator
        result = await _customValidator.ValidateAsync(context.User, request, result.Client);
        
        // create client in configuration system
        await _store.AddAsync(result.Client);

        // return response
        // todo: generate response from request + client
    }
}