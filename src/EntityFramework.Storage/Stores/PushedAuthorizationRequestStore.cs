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

/// <inheritdoc />
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
    
    /// <inheritdoc />
    public async Task ConsumeByHashAsync(string referenceValueHash)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("PersistedGrantStore.Remove");
        Logger.LogDebug("removing {referenceValueHash} pushed authorization from database", referenceValueHash);
        var numDeleted = await Context.PushedAuthorizationRequests
            .Where(par => par.ReferenceValueHash == referenceValueHash)
            .ExecuteDeleteAsync(CancellationTokenProvider.CancellationToken);
        if(numDeleted != 1)
        {
            Logger.LogWarning("attempted to remove {referenceValueHash} pushed authorization request because it was consumed, but no records were actually deleted.", referenceValueHash);
        }
    }

    /// <inheritdoc />
    public virtual async Task<Models.PushedAuthorizationRequest> GetByHashAsync(string referenceValueHash)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("PushedAuthorizationRequestStore.Get");
        
        var par = (await Context.PushedAuthorizationRequests
                .Where(x => x.ReferenceValueHash == referenceValueHash)
                .ToArrayAsync(CancellationTokenProvider.CancellationToken))
                .SingleOrDefault(x => x.ReferenceValueHash == referenceValueHash);
        var model = par?.ToModel();

        Logger.LogDebug("{referenceValueHash} pushed authorization found in database: {requestUriFound}", referenceValueHash, model != null);

        return model;
    }


    /// <inheritdoc />
    public virtual async Task StoreAsync(Models.PushedAuthorizationRequest par)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("PushedAuthorizationStore.Store");

        // If we're already tracking the PAR request, then we will update it.
        //
        // This is an optimization that allows us to use Store to add or update
        // without needing extra queries to determine if we need an add or
        // insert. It is relying on the fact that when we are updating, we will
        // have previously retrieved the PAR and it will be tracked in the context.
        var entry = Context.PushedAuthorizationRequests.Local.FindEntry("ReferenceValueHash", par.ReferenceValueHash);
        if(entry?.State is EntityState.Unchanged)
        {
            entry.CurrentValues.SetValues(par); // Not calling ToEntityHere so that we don't try to overwrite the id
        }
        // Otherwise, we will add it
        else
        {
            Context.PushedAuthorizationRequests.Add(par.ToEntity());
        }

        try
        {
            await Context.SaveChangesAsync(CancellationTokenProvider.CancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Logger.LogWarning("exception updating {referenceValueHash} pushed authorization in database: {error}", par.ReferenceValueHash, ex.Message);
        }
    }
}