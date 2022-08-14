using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Configuration;

public class DynamicClientRegistrationValidationResult
{
    public DynamicClientRegistrationValidationResult(Client client)
    {
        ArgumentNullException.ThrowIfNull(client);

        Client = client;
    }

    public DynamicClientRegistrationValidationResult(string error, string errorDescription)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(errorDescription);

        Error = error;
    }
    
    public Client? Client { get; }

    public string? Error { get; }
    
    public string? ErrorDescription { get; }

    public bool IsError => !string.IsNullOrWhiteSpace(Error);
}