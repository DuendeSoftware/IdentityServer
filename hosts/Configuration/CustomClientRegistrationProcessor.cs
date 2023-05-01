// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Configuration.RequestProcessing;
using Duende.IdentityServer.Configuration.Configuration;
using Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

namespace IdentityServerHost;

internal class CustomClientRegistrationProcessor : DynamicClientRegistrationRequestProcessor
{
    private readonly ICollection<Client> _clients;

    public CustomClientRegistrationProcessor(
        IdentityServerConfigurationOptions options,
        IClientConfigurationStore store,
        ICollection<Client> clients) : base(options, store)
    {
        _clients = clients;
    }


    protected override async Task<RequestProcessingStep> AddClientId(DynamicClientRegistrationValidatedRequest validatedRequest)
    {
        if (validatedRequest.OriginalRequest.Extensions.TryGetValue("client_id", out var clientIdParameter))
        {
            var clientId = clientIdParameter.ToString();
            if(_clients.Any(c => c.ClientId == clientId))
            {
                
                return new RequestProcessingStepFailure
                {
                    Error = "Duplicate client id",
                    ErrorDescription = "Attempt to register a client with a client id that has already been registered"
                };
            } 
            else
            {
                validatedRequest.Client.ClientId = clientId;
                return new RequestProcessingStepSuccess();
            }
        }
        return await base.AddClientId(validatedRequest);
    }

    protected override async Task<RequestProcessingStep<(Secret secret, string plainText)>> GenerateSecret(DynamicClientRegistrationValidatedRequest validatedRequest)
    {
         if(validatedRequest.OriginalRequest.Extensions.TryGetValue("client_secret", out var secretParam))
        {
            var secretPlainText = secretParam.ToString();
            var secret = new Secret(secretPlainText.Sha256());
            return new RequestProcessingStepSuccess<(Secret secret, string plainText)>
            {
                StepResult = (secret, secretPlainText)
            };
        }
        else
        {
            return await base.GenerateSecret(validatedRequest);
        }

    }
}