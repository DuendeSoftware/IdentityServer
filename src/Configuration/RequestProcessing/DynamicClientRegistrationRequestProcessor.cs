// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration.Configuration;
using Duende.IdentityServer.Configuration.Models;
using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
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
    public virtual async Task<IDynamicClientRegistrationResponse> ProcessAsync(
        DynamicClientRegistrationContext context)
    {
        var clientIdResult = await AddClientId(context);
        if(clientIdResult is DynamicClientRegistrationError clientIdFailure)
        {
            return clientIdFailure;
        }

        Secret? secret = null;
        string? plainText = null;
        var clientSecretResult = await AddClientSecret(context);
        if(clientSecretResult is DynamicClientRegistrationError clientSecretFailure)
        {
            return clientSecretFailure;
        }
        else if(clientSecretResult is SuccessfulStep)
        {
            if(context.Items.ContainsKey("secret") && context.Items["secret"] is Secret s &&
               context.Items.ContainsKey("plainText") && context.Items["plainText"] is string pt)
            {
                secret = s;
                plainText = pt;
            }
        }

        await _store.AddAsync(context.Client);

        return new DynamicClientRegistrationResponse(context.Request, context.Client)
        {
            ClientId = context.Client.ClientId,
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
    /// <param name="context">The dynamic client registration context, which
    /// includes the client model, the DCR request, and other contextual
    /// information.</param>
    /// <returns>A task that returns an <see cref="IStepResult"/>, which either
    /// represents that this step succeeded or failed.</returns>
    /// <remark> This method must set the "secret" and "plainText" properties of
    /// the context's Items dictionary.</remark>
    /// <returns>A task that returns an <see cref="IStepResult"/>, which either
    /// represents that this step succeeded or failed.</returns>
    
    protected virtual async Task<IStepResult> AddClientSecret(
        DynamicClientRegistrationContext context)
    {
        if (!context.Client.ClientSecrets.Any())
        {
            var (secret, plainText) = await GenerateSecret(context);
            context.Items["secret"] = secret;
            context.Items["plainText"] = plainText;
            context.Client.ClientSecrets.Add(secret);
        }
        return new SuccessfulStep();
    }

    /// <summary>
    /// Generates a secret for a dynamic client registration request.
    /// </summary>
    /// <param name="context">The dynamic client registration context, which
    /// includes the client model, the DCR request, and other contextual
    /// information.</param>
    /// <returns>A task that returns a tuple containing the generated secret and
    /// the plaintext of that secret.</returns>
    protected virtual Task<(Secret secret, string plainText)> GenerateSecret(
        DynamicClientRegistrationContext context)
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
    /// <param name="context">The dynamic client registration context, which
    /// includes the client model, the DCR request, and other contextual
    /// information.</param>
    /// <returns></returns>
    protected virtual Task<IStepResult> AddClientId(
        DynamicClientRegistrationContext context)
    {
        context.Client.ClientId = CryptoRandom.CreateUniqueId();
        return StepResult.Success();
    }
}