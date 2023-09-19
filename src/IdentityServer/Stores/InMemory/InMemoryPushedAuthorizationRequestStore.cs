// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

using Duende.IdentityServer.Models;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Stores;

/// <summary>
/// In-memory persisted grant store
/// </summary>
public class InMemoryPushedAuthorizationRequestStore : IPushedAuthorizationRequestStore
{
    private readonly ConcurrentDictionary<string, PushedAuthorizationRequest> _repository = new ConcurrentDictionary<string, PushedAuthorizationRequest>();

    /// <inheritdoc/>
    public Task StoreAsync(PushedAuthorizationRequest pushedAuthorizationRequest)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("InMemoryPersistedGrantStoreResponseGenerator.Store");
        
        _repository[pushedAuthorizationRequest.RequestUri] = pushedAuthorizationRequest;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<PushedAuthorizationRequest?> GetAsync(string requestUri)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("InMemoryPersistedGrantStoreResponseGenerator.Get");
        _repository.TryGetValue(requestUri, out var request);
        return Task.FromResult(request);
    }

    /// <inheritdoc/>
    public Task RemoveAsync(string requestUri)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("InMemoryPersistedGrantStoreResponseGenerator.Remove");
        _repository.TryRemove(requestUri, out _);
        return Task.CompletedTask;
    }
}