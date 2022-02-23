// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            await Context.SaveChangesAsync(CancellationTokenProvider.CancellationToken);
            Logger.LogDebug("new server side session {serverSideSessionKey} created in database", session.Key);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Logger.LogWarning("exception adding new server side session in database: {error}", ex.Message);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<ServerSideSession> GetSessionAsync(string key, CancellationToken cancellationToken = default)
    {
        var entity = (await Context.ServerSideSessions.AsNoTracking().Where(x => x.Key == key)
                .ToArrayAsync(CancellationTokenProvider.CancellationToken))
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

        Logger.LogDebug("server side session {serverSideSessionKey} found in database: {serverSideSessionKeyFound}", key, model != null);

        return model;
    }

    /// <inheritdoc/>
    public virtual async Task UpdateSessionAsync(ServerSideSession session, CancellationToken cancellationToken = default)
    {
        var entity = (await Context.ServerSideSessions.Where(x => x.Key == session.Key)
                .ToArrayAsync(CancellationTokenProvider.CancellationToken))
            .SingleOrDefault(x => x.Key == session.Key);

        if (entity == null)
        {
            Logger.LogDebug("no server side session {serverSideSessionKey} found in database. update failed", session.Key);
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
            await Context.SaveChangesAsync(CancellationTokenProvider.CancellationToken);
            Logger.LogDebug("server side session {serverSideSessionKey} updated in database", session.Key);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Logger.LogWarning("exception updating existing server side session {serverSideSessionKey} in database: {error}", session.Key, ex.Message);
        }
    }

    /// <inheritdoc/>
    public virtual async Task DeleteSessionAsync(string key, CancellationToken cancellationToken = default)
    {
        var entity = (await Context.ServerSideSessions.AsNoTracking().Where(x => x.Key == key)
                        .ToArrayAsync(CancellationTokenProvider.CancellationToken))
                    .SingleOrDefault(x => x.Key == key);

        if (entity == null)
        {
            Logger.LogDebug("no server side session {serverSideSessionKey} found in database. delete failed", key);
            return;
        }

        Context.ServerSideSessions.Remove(entity);

        try
        {
            await Context.SaveChangesAsync(CancellationTokenProvider.CancellationToken);
            Logger.LogDebug("server side session {serverSideSessionKey} deleted in database", key);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Logger.LogWarning("exception deleting existing server side session {serverSideSessionKey} in database: {error}", key, ex.Message);
        }
    }



    /// <inheritdoc/>
    public virtual async Task<IReadOnlyCollection<ServerSideSession>> GetSessionsAsync(SessionFilter filter, CancellationToken cancellationToken = default)
    {
        filter.Validate();

        var entities = await Filter(Context.ServerSideSessions.AsNoTracking().AsQueryable(), filter)
            .ToArrayAsync(CancellationTokenProvider.CancellationToken);
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

        Logger.LogDebug("{serverSideSessionCount} server side sessions found for {@filter}", results.Length, filter);

        return results;
    }

    /// <inheritdoc/>
    public virtual async Task DeleteSessionsAsync(SessionFilter filter, CancellationToken cancellationToken = default)
    {
        filter.Validate();

        var entities = await Filter(Context.ServerSideSessions.AsQueryable(), filter)
            .ToArrayAsync(CancellationTokenProvider.CancellationToken);
        entities = Filter(entities.AsQueryable(), filter).ToArray();

        Context.ServerSideSessions.RemoveRange(entities);

        try
        {
            await Context.SaveChangesAsync(CancellationTokenProvider.CancellationToken);
            Logger.LogDebug("removed {serverSideSessionCount} server side sessions from database for {@filter}", entities.Length, filter);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Logger.LogInformation("error removing {serverSideSessionCount} server side sessions from database for {@filter}: {error}", entities.Length, filter, ex.Message);
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
    public virtual async Task<QueryResult<ServerSideSession>> QuerySessionsAsync(SessionQuery filter = null, CancellationToken cancellationToken = default)
    {
        filter ??= new();
        if (filter.Page <= 0) filter.Page = 1;
        if (filter.Count <= 0) filter.Count = 25;

        var query = Context.ServerSideSessions.AsNoTracking().AsQueryable();

        if (!String.IsNullOrWhiteSpace(filter.DisplayName) ||
            !String.IsNullOrWhiteSpace(filter.SubjectId) ||
            !String.IsNullOrWhiteSpace(filter.SessionId))
        {
            query = query.Where(x =>
                (filter.SubjectId == null || x.SubjectId.Contains(filter.SubjectId)) ||
                (filter.SessionId == null || x.SessionId.Contains(filter.SessionId)) ||
                (filter.DisplayName == null || (x.DisplayName != null && x.DisplayName.Contains(filter.DisplayName) == true))
            );
        }

        var totalCount = query.Count();
        var countRequested = filter.Count;

        var totalPages = (int) Math.Max(1, Math.Ceiling(totalCount / (countRequested * 1.0)));
        var currentPage = Math.Min(filter.Page, totalPages);

        var results = await query.Skip(currentPage - 1).Take(countRequested)
            .Select(entity => new ServerSideSession
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
            })
            .ToArrayAsync();

        var result = new QueryResult<ServerSideSession>
        {
            Page = currentPage,
            CountRequested = countRequested,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Results = results
        };

        Logger.LogDebug("server side sessions found in the db {serverSideSessionCount}", results.Length);

        return result;
    }

}