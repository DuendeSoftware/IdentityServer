// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

using Duende.IdentityServer.Models;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Stores;

/// <summary>
/// In-memory implementation of the pushed authorization request store
/// </summary>
public class InMemoryPushedAuthorizationRequestStore : IPushedAuthorizationRequestStore
{
    private readonly ConcurrentDictionary<string, PushedAuthorizationRequest> _repository = new ConcurrentDictionary<string, PushedAuthorizationRequest>();

    /// <inheritdoc/>
    public Task StoreAsync(PushedAuthorizationRequest pushedAuthorizationRequest)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("InMemoryPushedAuthorizationRequestStore.Store");
        
        _repository[pushedAuthorizationRequest.ReferenceValue] = pushedAuthorizationRequest;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<PushedAuthorizationRequest?> GetAsync(string referenceValue)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("InMemoryPushedAuthorizationRequestStore.Get");
        _repository.TryGetValue(referenceValue, out var request);

        return Task.FromResult(request);
    }

    /// <inheritdoc/>
    public Task ConsumeAsync(string referenceValue)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("InMemoryPushedAuthorizationRequestStore.Remove");
        _repository.TryRemove(referenceValue, out _);
        return Task.CompletedTask;
    }
}