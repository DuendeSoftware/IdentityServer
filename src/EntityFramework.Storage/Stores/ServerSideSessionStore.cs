// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.EntityFramework.Extensions;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.EntityFramework.Stores;

/// <summary>
/// Implementation of IUserSessionStore thats uses EF.
/// </summary>
/// <seealso cref="IServerSideSessionStore" />
public class ServerSideSessionStore : IServerSideSessionStore
{
    /// <summary>
    /// The DbContext.
    /// </summary>
    protected readonly IPersistedGrantDbContext Context;

    /// <summary>
    /// The CancellationToken provider.
    /// </summary>
    protected readonly ICancellationTokenProvider CancellationTokenProvider;

    /// <summary>
    /// The logger.
    /// </summary>
    protected readonly ILogger<ServerSideSessionStore> Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerSideSessionStore"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationTokenProvider"></param>
    /// <exception cref="ArgumentNullException">context</exception>
    public ServerSideSessionStore(IPersistedGrantDbContext context, ILogger<ServerSideSessionStore> logger, ICancellationTokenProvider cancellationTokenProvider)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Logger = logger;
        CancellationTokenProvider = cancellationTokenProvider;
    }



    /// <inheritdoc/>
    public virtual async Task CreateSessionAsync(ServerSideSession session, CancellationToken cancellationToken = default)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("ServerSideSessionStore.CreateSession");
        
        cancellationToken = cancellationToken == CancellationToken.None ? CancellationTokenProvider.CancellationToken : cancellationToken;

        var entity = new Entities.ServerSideSession
        {
            Key = session.Key,
            Scheme = session.Scheme,
            SessionId = session.SessionId,
            SubjectId = session.SubjectId,
            DisplayName = session.DisplayName,
            Created = session.Created,
            Renewed = session.Renewed,
            Expires = session.Expires,
            Data = session.Ticket,
        };
        Context.ServerSideSessions.Add(entity);

        try
        {
            await Context.SaveChangesAsync(cancellationToken);
            Logger.LogDebug("Created new server-side session {serverSideSessionKey} in database", session.Key);
        }
        catch (DbUpdateException ex)
        {
            Logger.LogWarning("Exception creating new server-side session in database: {error}", ex.Message);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<ServerSideSession> GetSessionAsync(string key, CancellationToken cancellationToken = default)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("ServerSideSessionStore.GetSession");
        
        cancellationToken = cancellationToken == CancellationToken.None ? CancellationTokenProvider.CancellationToken : cancellationToken;

        var entity = (await Context.ServerSideSessions.AsNoTracking().Where(x => x.Key == key)
                .ToArrayAsync(cancellationToken))
            .SingleOrDefault(x => x.Key == key);

        var model = default(ServerSideSession);
        if (entity != null)
        {
            model = new ServerSideSession
            {
                Key = entity.Key,
                Scheme = entity.Scheme,
                SubjectId = entity.SubjectId,
                SessionId = entity.SessionId,
                DisplayName = entity.DisplayName,
                Created = entity.Created,
                Renewed = entity.Renewed,
                Expires = entity.Expires,
                Ticket = entity.Data,
            };
        }

        Logger.LogDebug("Found server-side session {serverSideSessionKey} in database: {serverSideSessionKeyFound}", key, model != null);

        return model;
    }

    /// <inheritdoc/>
    public virtual async Task UpdateSessionAsync(ServerSideSession session, CancellationToken cancellationToken = default)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("ServerSideSessionStore.UpdateSession");
        
        cancellationToken = cancellationToken == CancellationToken.None ? CancellationTokenProvider.CancellationToken : cancellationToken;

        var entity = (await Context.ServerSideSessions.Where(x => x.Key == session.Key)
                .ToArrayAsync(cancellationToken))
            .SingleOrDefault(x => x.Key == session.Key);

        if (entity == null)
        {
            Logger.LogDebug("No server-side session {serverSideSessionKey} found in database. Update failed.", session.Key);
            return;
        }

        entity.Scheme = session.Scheme;
        entity.SubjectId = session.SubjectId;
        entity.SessionId = session.SessionId;
        entity.DisplayName = session.DisplayName;
        entity.Created = session.Created;
        entity.Renewed = session.Renewed;
        entity.Expires = session.Expires;
        entity.Data = session.Ticket;

        try
        {
            await Context.SaveChangesAsync(cancellationToken);
            Logger.LogDebug("Updated server-side session {serverSideSessionKey} in database", session.Key);
        }
        catch (DbUpdateException ex)
        {
            Logger.LogWarning("Exception updating existing server side session {serverSideSessionKey} in database: {error}", session.Key, ex.Message);
        }
    }

    /// <inheritdoc/>
    public virtual async Task DeleteSessionAsync(string key, CancellationToken cancellationToken = default)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("ServerSideSessionStore.DeleteSession");
        
        cancellationToken = cancellationToken == CancellationToken.None ? CancellationTokenProvider.CancellationToken : cancellationToken;
        
        var entity = (await Context.ServerSideSessions.Where(x => x.Key == key)
                        .ToArrayAsync(cancellationToken))
                    .SingleOrDefault(x => x.Key == key);

        if (entity == null)
        {
            Logger.LogDebug("No server side session {serverSideSessionKey} found in database. Delete failed.", key);
            return;
        }

        Context.ServerSideSessions.Remove(entity);

        try
        {
            await Context.SaveChangesAsync(cancellationToken);
            Logger.LogDebug("Deleted server-side session {serverSideSessionKey} in database", key);
        }
        catch (DbUpdateException ex)
        {
            Logger.LogWarning("Exception deleting server-side session {serverSideSessionKey} in database: {error}", key, ex.Message);
        }
    }



    /// <inheritdoc/>
    public virtual async Task<IReadOnlyCollection<ServerSideSession>> GetSessionsAsync(SessionFilter filter, CancellationToken cancellationToken = default)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("ServerSideSessionStore.GetSessions");
        
        cancellationToken = cancellationToken == CancellationToken.None ? CancellationTokenProvider.CancellationToken : cancellationToken;
        
        filter.Validate();

        var entities = await Filter(Context.ServerSideSessions.AsNoTracking().AsQueryable(), filter)
            .ToArrayAsync(cancellationToken);
        entities = Filter(entities.AsQueryable(), filter).ToArray();

        var results = entities.Select(entity => new ServerSideSession
        {
            Key = entity.Key,
            Scheme = entity.Scheme,
            SubjectId = entity.SubjectId,
            SessionId = entity.SessionId,
            DisplayName = entity.DisplayName,
            Created = entity.Created,
            Renewed = entity.Renewed,
            Expires = entity.Expires,
            Ticket = entity.Data,
        }).ToArray();

        Logger.LogDebug("Found {serverSideSessionCount} server-side sessions for {@filter}", results.Length, filter);

        return results;
    }

    /// <inheritdoc/>
    public virtual async Task DeleteSessionsAsync(SessionFilter filter, CancellationToken cancellationToken = default)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("ServerSideSessionStore.DeleteSessions");
        
        cancellationToken = cancellationToken == CancellationToken.None ? CancellationTokenProvider.CancellationToken : cancellationToken;
        
        filter.Validate();

        var entities = await Filter(Context.ServerSideSessions.AsQueryable(), filter)
            .ToArrayAsync(cancellationToken);
        entities = Filter(entities.AsQueryable(), filter).ToArray();

        Context.ServerSideSessions.RemoveRange(entities);

        try
        {
            await Context.SaveChangesAsync(cancellationToken);
            Logger.LogDebug("Removed {serverSideSessionCount} server-side sessions from database for {@filter}", entities.Length, filter);
        }
        catch (DbUpdateException ex)
        {
            Logger.LogInformation("Error removing {serverSideSessionCount} server-side sessions from database for {@filter}: {error}", entities.Length, filter, ex.Message);
        }
    }

    private IQueryable<Entities.ServerSideSession> Filter(IQueryable<Entities.ServerSideSession> query, SessionFilter filter)
    {
        if (!String.IsNullOrWhiteSpace(filter.SubjectId))
        {
            query = query.Where(x => x.SubjectId == filter.SubjectId);
        }
        if (!String.IsNullOrWhiteSpace(filter.SessionId))
        {
            query = query.Where(x => x.SessionId == filter.SessionId);
        }

        return query;
    }


    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ServerSideSession>> GetAndRemoveExpiredSessionsAsync(int count, CancellationToken cancellationToken = default)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("ServerSideSessionStore.GetAndRemoveExpiredSessions");
        
        cancellationToken = cancellationToken == CancellationToken.None ? CancellationTokenProvider.CancellationToken : cancellationToken;
        
        var entities = await Context.ServerSideSessions
                            .Where(x => x.Expires < DateTime.UtcNow)
                            .OrderBy(x => x.Id)
                            .Take(count)
                            .ToArrayAsync(cancellationToken);

        if (entities.Length > 0)
        {
            Context.ServerSideSessions.RemoveRange(entities);
            
            var list = await Context.SaveChangesWithConcurrencyCheckAsync<Entities.ServerSideSession>(Logger, cancellationToken);
            entities = entities.Except(list).ToArray();
            
            Logger.LogDebug("Found and removed {serverSideSessionCount} expired server-side sessions", entities.Length);
        }

        var results = entities.Select(entity => new ServerSideSession
        {
            Key = entity.Key,
            Scheme = entity.Scheme,
            SubjectId = entity.SubjectId,
            SessionId = entity.SessionId,
            DisplayName = entity.DisplayName,
            Created = entity.Created,
            Renewed = entity.Renewed,
            Expires = entity.Expires,
            Ticket = entity.Data,
        }).ToArray();

        return results;
    }

    /// <inheritdoc/>
    public virtual async Task<QueryResult<ServerSideSession>> QuerySessionsAsync(SessionQuery filter = null, CancellationToken cancellationToken = default)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("ServerSideSessionStore.QuerySessions");
        
        cancellationToken = cancellationToken == CancellationToken.None ? CancellationTokenProvider.CancellationToken : cancellationToken;
        
        // it's possible that this implementation could have been done differently (e.g. use the page number for the token)
        // but it was done deliberately in such a way to allow document databases to mimic the logic
        // and omit features not supported (such as total count, total pages, and current page)
        // given that this is intended to be used as an administrative UI feature, performance was less of a concern

        filter ??= new();

        // these are the ids of first and last items in the prior results
        // stored as "x,y" in the filter.ResultsToken.
        var first = 0;
        var last = 0;

        if (filter.ResultsToken != null)
        {
            var parts = filter.ResultsToken.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts != null && parts.Length == 2)
            {
                Int32.TryParse(parts[0], out first);
                Int32.TryParse(parts[1], out last);
            }
        }

        var countRequested = filter.CountRequested;
        if (countRequested <= 0) countRequested = 25;

        var query = Context.ServerSideSessions.AsNoTracking().AsQueryable();

        if (!String.IsNullOrWhiteSpace(filter.DisplayName) ||
            !String.IsNullOrWhiteSpace(filter.SubjectId) ||
            !String.IsNullOrWhiteSpace(filter.SessionId))
        {
            query = query.Where(x =>
                (filter.SubjectId == null || x.SubjectId.Contains(filter.SubjectId)) &&
                (filter.SessionId == null || x.SessionId.Contains(filter.SessionId)) &&
                (filter.DisplayName == null || (x.DisplayName != null && x.DisplayName.Contains(filter.DisplayName) == true))
            );
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int) Math.Max(1, Math.Ceiling(totalCount / (countRequested * 1.0)));
        
        var currPage = 1;

        var hasNext = false;
        var hasPrev = false;
        Entities.ServerSideSession[] items = null;

        if (filter.RequestPriorResults)
        {
            // sets query at the prior record from the last results, but in reverse order
            items = await query.OrderByDescending(x => x.Id)
                .Where(x => x.Id < first)
                // and we +1 to see if there's a prev page
                .Take(countRequested + 1)
                .ToArrayAsync(cancellationToken);

            // put them back into ID order
            items = items.OrderBy(x => x.Id).ToArray(); 

            // if we have the one extra, we have a prev page
            hasPrev = items.Length > countRequested;

            if (hasPrev)
            {
                // omit prev results entry
                items = items.Skip(1).ToArray();
            }

            // how many are to the right of these results?
            if (items.Any())
            {
                var postCountId = items[items.Length - 1].Id;
                var postCount = await query.CountAsync(x => x.Id > postCountId, cancellationToken);
                hasNext = postCount > 0;
                currPage = totalPages - (int) Math.Ceiling((1.0 * postCount) / countRequested);
            }

            if (currPage == 1 && hasNext && items.Length < countRequested)
            {
                // this handles when we went back and are now at the beginning but items were deleted.
                // we need to start over and re-query from the beginning.
                filter.ResultsToken = null;
                filter.RequestPriorResults = false;
                return await QuerySessionsAsync(filter, cancellationToken);
            }
        }
        else
        {
            items = await query.OrderBy(x => x.Id)
                // if lastResultsId is zero, then this will just start at beginning
                .Where(x => x.Id > last)
                // and we +1 to see if there's a next page
                .Take(countRequested + 1)
                .ToArrayAsync(cancellationToken);

            // if we have the one extra, we have a next page
            hasNext = items.Length > countRequested;

            if (hasNext)
            {
                // omit next results entry
                items = items.SkipLast(1).ToArray();
            }

            // how many are to the left of these results?
            if (items.Any())
            {
                var priorCountId = items[0].Id;
                var priorCount = await query.CountAsync(x => x.Id < last, cancellationToken);
                hasPrev = priorCount > 0;
                currPage = 1 + (int) Math.Ceiling((1.0 * priorCount) / countRequested);
            }
        }

        // this handles prior entries being deleted since paging begun
        if (currPage <= 1)
        {
            currPage = 1;
            hasPrev = false;
        }

        string resultsToken = null;
        if (items.Length > 0)
        {
            resultsToken = $"{items[0].Id},{items[items.Length - 1].Id}";
        }
        else
        {
            // no results, so we're out of bounds
            hasPrev = false;
            hasNext = false;
            totalCount = 0;
            totalPages = 0;
            currPage = 0;
        }

        var results = items.Select(entity => new ServerSideSession
        {
            Key = entity.Key,
            Scheme = entity.Scheme,
            SubjectId = entity.SubjectId,
            SessionId = entity.SessionId,
            DisplayName = entity.DisplayName,
            Created = entity.Created,
            Renewed = entity.Renewed,
            Expires = entity.Expires,
            Ticket = entity.Data,
        }).ToArray();

        var result = new QueryResult<ServerSideSession>
        {
            ResultsToken = resultsToken,
            HasNextResults = hasNext,
            HasPrevResults = hasPrev,
            TotalCount = totalCount,
            TotalPages = totalPages,
            CurrentPage = currPage,
            Results = results
        };

        Logger.LogDebug("Found {serverSideSessionCount} server-side sessions in database", results.Length);

        return result;
    }
}

/*


        filter ??= new();

        Int32.TryParse(filter.ResultsToken, out var lastPageNumber);
        if (lastPageNumber < 0) lastPageNumber = 0;

        var countRequested = filter.CountRequested;
        if (countRequested <= 0) countRequested = 25;

        var query = _store.Values.AsQueryable();
        
        if (!String.IsNullOrWhiteSpace(filter.DisplayName) || 
            !String.IsNullOrWhiteSpace(filter.SubjectId) ||
            !String.IsNullOrWhiteSpace(filter.SessionId))
        {
            query = query.Where(x =>
                (filter.SubjectId == null || x.SubjectId.Contains(filter.SubjectId, StringComparison.OrdinalIgnoreCase)) ||
                (filter.SessionId == null || x.SessionId.Contains(filter.SessionId, StringComparison.OrdinalIgnoreCase)) ||
                (filter.DisplayName == null || (x.DisplayName != null && x.DisplayName.Contains(filter.DisplayName, StringComparison.OrdinalIgnoreCase) == true))
            );
        }
        
        var totalCount = query.Count();
        var totalPages = (int) Math.Max(1, Math.Ceiling(totalCount / (countRequested * 1.0)));

        var ordered = query = query.OrderBy(x => x.Key);
        if (filter.RequestPriorResults)
        {
            // sets query at the prior block from the last page
            if (lastPageNumber > 1)
            {
                query = ordered.Skip((lastPageNumber - 1) * countRequested);
                lastPageNumber--;
            }
            else
            {
                // nothing if you ask for stuff before page 1
                query = Enumerable.Empty<ServerSideSession>().AsQueryable();
            }
        }
        else
        {
            // this skips the last results
            query = ordered.Skip(lastPageNumber * countRequested);
            lastPageNumber++;
        }

        var results = query.Take(countRequested)
            .Select(x => x.Clone())
            .ToArray();

        var token = lastPageNumber.ToString();
        if (lastPageNumber < 1) token = null;
        if (lastPageNumber > totalPages) token = null;
        
        var result = new QueryResult<ServerSideSession>
        {
            ResultsToken = token,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Results = results
        };


*/