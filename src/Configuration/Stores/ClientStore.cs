using Duende.IdentityServer.Configuration.Repositories;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;

namespace Duende.IdentityServer.Configuration.Stores;

/// <summary>
/// Implement of IClientStore that uses <see cref="IClientRepository"/>
/// </summary>
public class ClientStore : IClientStore
{
    private readonly IClientRepository _repository;
    private readonly ICancellationTokenProvider _cancellationTokenProvider;

    public ClientStore(IClientRepository repository, ICancellationTokenProvider cancellationTokenProvider)
    {
        _repository = repository;
        _cancellationTokenProvider = cancellationTokenProvider;
    }

    public virtual async Task<Client?> FindClientByIdAsync(string clientId)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("ClientStore.FindClientById");
        activity?.SetTag(Tracing.Properties.ClientId, clientId);

        var client = await _repository.Read(clientId, _cancellationTokenProvider.CancellationToken);

        return client;
    }
}