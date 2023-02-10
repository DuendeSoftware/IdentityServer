using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Configuration;

public class DummyClientConfigurationStore : IClientConfigurationStore
{
    public Task AddAsync(Client client)
    {
        return Task.CompletedTask;
    }
}