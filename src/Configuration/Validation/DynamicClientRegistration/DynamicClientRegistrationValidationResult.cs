// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

public class DynamicClientRegistrationValidationResult { }

public class DynamicClientRegistrationValidationError : DynamicClientRegistrationValidationResult
{
    public DynamicClientRegistrationValidationError(string error, string errorDescription)
    {
        Error = error;
        ErrorDescription = errorDescription;
    }

    public string Error { get; set; }
    public string ErrorDescription { get; set; }
}

public class DynamicClientRegistrationValidatedRequest : DynamicClientRegistrationValidationResult
{
    public DynamicClientRegistrationValidatedRequest(Client client, DynamicClientRegistrationRequest originalRequest)
    {
        Client = client;
        OriginalRequest = originalRequest;
    }

    public Client Client { get; set; }
    public DynamicClientRegistrationRequest OriginalRequest { get; set; }
}