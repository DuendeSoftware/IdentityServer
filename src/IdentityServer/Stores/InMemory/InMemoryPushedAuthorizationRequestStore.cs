// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

using Duende.IdentityServer.Models;
using System;
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
        using var activity = Tracing.StoreActivitySource.StartActivity("InMemoryPushedAuthorizationRequestStore.Store");
        
        _repository[pushedAuthorizationRequest.ReferenceValue] = pushedAuthorizationRequest;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<PushedAuthorizationRequest?> GetAsync(string referenceValue)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("InMemoryPushedAuthorizationRequestStore.Get");
        _repository.TryGetValue(referenceValue, out var request);

        return Task.FromResult(request switch
        {
            { Consumed: false } => request,
            _ => null
        });
    }

    /// <inheritdoc/>
    public Task RemoveAsync(string referenceValue)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("InMemoryPushedAuthorizationRequestStore.Remove");
        _repository.TryRemove(referenceValue, out _);
        return Task.CompletedTask;
    }

    public Task ConsumeAsync(string referenceValue)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("InMemoryPushedAuthorizationRequestStore.Consume");
        
        if(_repository.TryGetValue(referenceValue, out var request))
        {
            request.Consumed = true;
        }
        return Task.CompletedTask;
    }

    // TODO - Add a higher level abstraction to perform this operation
    // Try to avoid unnecessary store operations when we do that
    public Task RotateAsync(string oldReferenceValue, string newReferenceValue)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("InMemoryPushedAuthorizationRequestStore.Consume");
        
        if(_repository.TryGetValue(oldReferenceValue, out var oldRequest))
        {
            _repository[newReferenceValue] = new PushedAuthorizationRequest
            {
                ReferenceValue = newReferenceValue,
                Parameters = oldRequest.Parameters,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15), // TODO - This should come from config, possibly passed in. 
                Consumed = false
            };
        }
        return Task.CompletedTask;
    }
}