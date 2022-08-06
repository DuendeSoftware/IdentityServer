using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Configuration;

public class DynamicClientRegistrationValidationResult
{
    public DynamicClientRegistrationValidationResult(Client client)
    {
        ArgumentNullException.ThrowIfNull(client);

        Client = client;
    }

    public DynamicClientRegistrationValidationResult(string error)
    {
        ArgumentNullException.ThrowIfNull(error);

        Error = error;
    }
    
    public Client? Client { get; }

    public string? Error { get; }

    public bool IsError => !string.IsNullOrWhiteSpace(Error);
}