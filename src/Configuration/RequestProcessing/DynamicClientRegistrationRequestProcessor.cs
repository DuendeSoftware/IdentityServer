// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration.Configuration;
using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;
using Duende.IdentityServer.Models;
using IdentityModel;

namespace Duende.IdentityServer.Configuration.RequestProcessing;

/// <inheritdoc />
public class DynamicClientRegistrationRequestProcessor : IDynamicClientRegistrationRequestProcessor
{
    private readonly IdentityServerConfigurationOptions _options;
    private readonly IClientConfigurationStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicClientRegistrationRequestProcessor"/> class.
    /// </summary>
    /// <param name="options">The IdentityServer.Configuration options.</param>
    /// <param name="store">The client configuration store.</param>
    public DynamicClientRegistrationRequestProcessor(
        IdentityServerConfigurationOptions options, 
        IClientConfigurationStore store)
    {
        _options = options;
        _store = store;
    }


    /// <inheritdoc />
    public virtual async Task<DynamicClientRegistrationResponse> ProcessAsync(
        DynamicClientRegistrationValidatedRequest validatedRequest)
    {
        await AddClientId(validatedRequest);

        var (secret, plainText) = await AddClientSecret(validatedRequest) switch
        {
            (Secret s, string p) => (s, p),
            null => (null, null)
        };

        await _store.AddAsync(validatedRequest.Client);

        return new DynamicClientRegistrationResponse(validatedRequest.OriginalRequest)
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

    /// <summary>
    /// Adds a client secret to a dynamic client registration request.
    /// </summary>
    /// <param name="validatedRequest">The validated dynamic client registration request.</param>
    /// <returns>A tuple containing the added secret and its plaintext representation, or null if no secret was added.</returns>
    protected virtual async Task<(Secret secret, string plainText)?> AddClientSecret(
        DynamicClientRegistrationValidatedRequest validatedRequest)
    {
        if (!validatedRequest.Client.ClientSecrets.Any())
        {
            var (secret, plainText) = await GenerateSecret(validatedRequest);
            validatedRequest.Client.ClientSecrets.Add(secret);
            return (secret, plainText);
        }
        return null;
    }

    /// <summary>
    /// Generates a secret for a dynamic client registration request.
    /// </summary>
    /// <param name="validatedRequest">The validated request to generate a secret for.</param>
    /// <returns>A tuple containing the generated secret and its plaintext representation.</returns>
    protected virtual Task<(Secret secret, string plainText)> GenerateSecret(
        DynamicClientRegistrationValidatedRequest validatedRequest)
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

    /// <summary>
    /// Generates a client ID and adds it to the validatedRequest's client
    /// model.
    /// </summary>
    /// <param name="validatedRequest">The request whose client will have an Id
    /// generated.</param>
    protected virtual Task AddClientId(
        DynamicClientRegistrationValidatedRequest validatedRequest)
    {
        validatedRequest.Client.ClientId = CryptoRandom.CreateUniqueId();
        return Task.CompletedTask;
    }
}