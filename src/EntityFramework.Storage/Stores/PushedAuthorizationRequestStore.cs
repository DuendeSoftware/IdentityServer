// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Services;

namespace Duende.IdentityServer.EntityFramework.Stores;

public class PushedAuthorizationRequestStore : IPushedAuthorizationRequestStore
{

    /// <summary>
    /// The DbContext.
    /// </summary>
    protected readonly IPersistedGrantDbContext Context;

    /// <summary>
    /// The CancellationToken service.
    /// </summary>
    protected readonly ICancellationTokenProvider CancellationTokenProvider;
        
    /// <summary>
    /// The logger.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PushedAuthorizationRequestStore"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationTokenProvider"></param>
    public PushedAuthorizationRequestStore(IPersistedGrantDbContext context, ILogger<PushedAuthorizationRequestStore> logger, ICancellationTokenProvider cancellationTokenProvider)
    {
        Context = context;
        Logger = logger;
        CancellationTokenProvider = cancellationTokenProvider;
    }

    public async Task ConsumeAsync(string referenceValue)
    {
        await Context.PushedAuthorizationRequests
            .Where(par => par.ReferenceValue == referenceValue)
            .ExecuteUpdateAsync(setters => 
                setters.SetProperty(par => par.Consumed, true), 
                CancellationTokenProvider.CancellationToken);
    }

    public virtual async Task<Models.PushedAuthorizationRequest> GetAsync(string referenceValue)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("PushedAuthorizationRequestStore.Get");
        
        var par = (await Context.PushedAuthorizationRequests
                .AsNoTracking().Where(x => x.ReferenceValue == referenceValue)
                .ToArrayAsync(CancellationTokenProvider.CancellationToken))
                .SingleOrDefault(x => x.ReferenceValue == referenceValue);
        var model = par?.ToModel();

        // REVIEW - is it safe to log the reference value?
        Logger.LogDebug("{referenceValue} pushed authorization found in database: {requestUriFound}", par.ReferenceValue, model != null);

        return model;
    }

    public virtual async Task RemoveAsync(string referenceValue)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("PersistedGrantStore.Remove");
        Logger.LogDebug("removing {referenceValue} pushed authorization from database", referenceValue);
        var numDeleted = await Context.PushedAuthorizationRequests
            .Where(par => par.ReferenceValue == referenceValue)
            .ExecuteDeleteAsync(CancellationTokenProvider.CancellationToken);
        if(numDeleted != 1)
        {
            // TODO - is this an error? Something weird is probably going on.
        }
    }

    public Task RotateAsync(string oldReferenceValue, string newReferenceValue)
    {
        // TODO (if we like this approach)
        throw new System.NotImplementedException();
    }

    public virtual async Task StoreAsync(Models.PushedAuthorizationRequest par)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("PushedAuthorizationStore.Store");
        
        Context.PushedAuthorizationRequests.Add(par.ToEntity());
        try
        {
            await Context.SaveChangesAsync(CancellationTokenProvider.CancellationToken);
        }
        // REVIEW - Is this exception possible, since we don't try to load (and then update) an existing entity?
        // I think it isn't, but what happens if we somehow two calls to StoreAsync with the same PAR are made?
        catch (DbUpdateConcurrencyException ex)
        {
            Logger.LogWarning("exception updating {referenceValue} pushed authorization in database: {error}", par.ReferenceValue, ex.Message);
        }
    }
}