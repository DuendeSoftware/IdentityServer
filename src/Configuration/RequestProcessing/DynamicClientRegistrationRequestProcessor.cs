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
    public virtual async Task<IDynamicClientRegistrationResponse> ProcessAsync(
        DynamicClientRegistrationValidatedRequest validatedRequest)
    {
        var clientIdResult = await AddClientId(validatedRequest);
        if(clientIdResult is RequestProcessingStepFailure clientIdFailure)
        {
            return new DynamicClientRegistrationErrorResponse
            {
                Error = clientIdFailure.Error,
                ErrorDescription = clientIdFailure.ErrorDescription
            };
        }

        Secret? secret = null;
        string? plainText = null;
        var clientSecretResult = await AddClientSecret(validatedRequest);
        if(clientSecretResult is RequestProcessingStepFailure<(Secret Secret, string PlainText)> clientSecretFailure)
        {
            return new DynamicClientRegistrationErrorResponse
            {
                Error = clientSecretFailure.Error,
                ErrorDescription = clientSecretFailure.ErrorDescription
            };
        }
        else if(clientSecretResult is RequestProcessingStepSuccess<(Secret Secret, string PlainText)> clientSecretSuccess)
        {
            secret = clientSecretSuccess.StepResult.Secret;
            plainText = clientSecretSuccess.StepResult.PlainText;
        }

        await _store.AddAsync(validatedRequest.Client);

        return new DynamicClientRegistrationResponse(validatedRequest.OriginalRequest, validatedRequest.Client)
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
    protected virtual async Task<RequestProcessingStep<(Secret secret, string plainText)>> AddClientSecret(
        DynamicClientRegistrationValidatedRequest validatedRequest)
    {
        if (!validatedRequest.Client.ClientSecrets.Any())
        {
            var result = await GenerateSecret(validatedRequest);
            if(result is RequestProcessingStepSuccess<(Secret secret, string plainText)> success)
            {
                var (secret, _) = success.StepResult;
                validatedRequest.Client.ClientSecrets.Add(secret);
            }
            return result;
        }
        return new RequestProcessingStepSuccess<(Secret secret, string plainText)>();
    }

    /// <summary>
    /// Generates a secret for a dynamic client registration request.
    /// </summary>
    /// <param name="validatedRequest">The validated request to generate a secret for.</param>
    /// <returns>A tuple containing the generated secret and its plaintext representation.</returns>
    protected virtual Task<RequestProcessingStep<(Secret secret, string plainText)>> GenerateSecret(
        DynamicClientRegistrationValidatedRequest validatedRequest)
    {
        var plainText = CryptoRandom.CreateUniqueId();

        DateTime? lifetime = _options.DynamicClientRegistration.SecretLifetime switch
        {
            null => null,
            TimeSpan t => DateTime.UtcNow.Add(t)
        };

        var secret = new Secret(plainText.ToSha256(), lifetime);

        var success = new RequestProcessingStepSuccess<(Secret secret, string plainText)>
        {
            StepResult = (secret, plainText)
        };

        return Task.FromResult<RequestProcessingStep<(Secret secret, string plainText)>>(success);
    }

    /// <summary>
    /// Generates a client ID and adds it to the validatedRequest's client
    /// model.
    /// </summary>
    /// <param name="validatedRequest">The request whose client will have an Id
    /// generated.</param>
    /// <returns>
    /// True if a client id was successfully added, and false otherwise
    /// </returns>
    protected virtual Task<RequestProcessingStep> AddClientId(
        DynamicClientRegistrationValidatedRequest validatedRequest)
    {
        validatedRequest.Client.ClientId = CryptoRandom.CreateUniqueId();
        return Task.FromResult<RequestProcessingStep>(new RequestProcessingStepSuccess());
    }
}