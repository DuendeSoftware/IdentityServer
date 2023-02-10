using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Configuration;

public interface IClientConfigurationStore
{
    Task AddAsync(Client client);
}