using Duende.IdentityServer.Models;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Stores.Empty;

internal class EmptyClientStore : IClientStore
{
    public Task<Client> FindClientByIdAsync(string clientId)
    {
        return Task.FromResult<Client>(null);
    }
}
