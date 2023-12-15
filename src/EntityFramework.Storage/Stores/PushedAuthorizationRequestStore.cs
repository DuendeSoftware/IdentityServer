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
                .AsNoTracking().Where(x => x.ReferenceValueHash == referenceValueHash)
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
        
        Context.PushedAuthorizationRequests.Add(par.ToEntity());
        try
        {
            await Context.SaveChangesAsync(CancellationTokenProvider.CancellationToken);
        }
        // REVIEW - Is this exception possible, since we don't try to load (and then update) an existing entity?
        // I think it isn't, but what happens if we somehow two calls to StoreAsync with the same PAR are made?
        catch (DbUpdateConcurrencyException ex)
        {
            Logger.LogWarning("exception updating {referenceValueHash} pushed authorization in database: {error}", par.ReferenceValueHash, ex.Message);
        }
    }
}