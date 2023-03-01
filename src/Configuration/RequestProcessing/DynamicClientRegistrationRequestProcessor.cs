using Duende.IdentityServer.Configuration.Configuration;
using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;
using Duende.IdentityServer.Models;
using IdentityModel;

namespace Duende.IdentityServer.Configuration;

public class DynamicClientRegistrationRequestProcessor : IDynamicClientRegistrationRequestProcessor
{
    private readonly IdentityServerConfigurationOptions _options;
    private readonly IClientConfigurationStore _store;

    public DynamicClientRegistrationRequestProcessor(IdentityServerConfigurationOptions options, IClientConfigurationStore store)
    {
        _options = options;
        _store = store;
    }


    public virtual async Task<DynamicClientRegistrationResponse> ProcessAsync(DynamicClientRegistrationValidatedRequest validatedRequest)
    {
        var (secret, plainText) = await AddClientSecret(validatedRequest) switch
        {
            (Secret s, string p) => (s, p),
            null => (null, null)
        };

        // create client in configuration system
        await _store.AddAsync(validatedRequest.Client);

        return new DynamicClientRegistrationResponse(validatedRequest.Original)
        {
            ClientId = validatedRequest.Client.ClientId,
            ClientSecret = plainText,
            ClientSecretExpiresAt = secret switch
            {
                null => null,
                { Expiration: null } => 0,
                { Expiration: DateTime e } => new DateTimeOffset(e).ToUnixTimeSeconds()
            }
        };
    }

    public virtual async Task<(Secret secret, string plainText)?> AddClientSecret(DynamicClientRegistrationValidatedRequest validatedRequest)
    {
        if (!validatedRequest.Client.ClientSecrets.Any())
        {
            var (secret, plainText) = await GenerateSecret();
            validatedRequest.Client.ClientSecrets.Add(secret);
            return (secret, plainText);
        }
        return null;
    }

     public virtual Task<(Secret secret, string plainText)> GenerateSecret()
    {
        var plainText = CryptoRandom.CreateUniqueId();

        DateTime? lifetime = _options.DynamicClientRegistration.SecretLifetime switch
        {
            null => null,
            TimeSpan t => DateTime.UtcNow.Add(t)
        };

        var secret = new Secret(plainText.ToSha256(), lifetime);

        return Task.FromResult((secret, plainText));
    }
}



    
