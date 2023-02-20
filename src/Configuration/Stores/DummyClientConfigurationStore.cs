using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Configuration;

// TODO - Probably remove this, unless useful in a test?
public class DummyClientConfigurationStore : IClientConfigurationStore
{
    public Task AddAsync(Client client)
    {
        return Task.CompletedTask;
    }
}