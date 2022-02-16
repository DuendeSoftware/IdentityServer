// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.Services;
using Duende.SessionManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.EntityFramework.Stores;

/// <summary>
/// Implementation of IUserSessionStore thats uses EF.
/// </summary>
/// <seealso cref="IUserSessionStore" />
public class UserSessionStore : IUserSessionStore
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
    protected readonly ILogger<UserSessionStore> Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSessionStore"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationTokenProvider"></param>
    /// <exception cref="ArgumentNullException">context</exception>
    public UserSessionStore(IPersistedGrantDbContext context, ILogger<UserSessionStore> logger, ICancellationTokenProvider cancellationTokenProvider)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Logger = logger;
        CancellationTokenProvider = cancellationTokenProvider;
    }

    /// <inheritdoc/>
    public virtual async Task CreateUserSessionAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        var entity = new Entities.UserSession
        {
            Key = session.Key,
            Scheme = session.Scheme,
            SessionId = session.SessionId,
            SubjectId = session.SubjectId,
            DisplayName = session.DisplayName,
            Created = session.Created,
            Renewed = session.Renewed,
            Expires = session.Expires,
            Ticket = session.Ticket,
        };
        Context.UserSessions.Add(entity);

        try
        {
            await Context.SaveChangesAsync(CancellationTokenProvider.CancellationToken);
            Logger.LogDebug("new user session {userSessionKey} created in database", session.Key);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Logger.LogWarning("exception adding new user session in database: {error}", ex.Message);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<UserSession> GetUserSessionAsync(string key, CancellationToken cancellationToken = default)
    {
        var entity = (await Context.UserSessions.AsNoTracking().Where(x => x.Key == key)
                .ToArrayAsync(CancellationTokenProvider.CancellationToken))
            .SingleOrDefault(x => x.Key == key);

        var model = default(UserSession);
        if (entity != null)
        {
            model = new UserSession
            {
                Key = entity.Key,
                Scheme = entity.Scheme,
                SubjectId = entity.SubjectId,
                SessionId = entity.SessionId,
                DisplayName = entity.DisplayName,
                Created = entity.Created,
                Renewed = entity.Renewed,
                Expires = entity.Expires,
                Ticket = entity.Ticket,
            };
        }

        Logger.LogDebug("user session {userSessionKey} found in database: {userSessionKeyFound}", key, model != null);

        return model;
    }

    /// <inheritdoc/>
    public virtual async Task UpdateUserSessionAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        var entity = (await Context.UserSessions.AsNoTracking().Where(x => x.Key == session.Key)
                .ToArrayAsync(CancellationTokenProvider.CancellationToken))
            .SingleOrDefault(x => x.Key == session.Key);

        if (entity == null)
        {
            Logger.LogDebug("no user session {userSessionKey} found in database. update failed", session.Key);
            return;
        }

        entity.Scheme = session.Scheme;
        entity.SubjectId = session.SubjectId;
        entity.SessionId = session.SessionId;
        entity.DisplayName = session.DisplayName;
        entity.Created = session.Created;
        entity.Renewed = session.Renewed;
        entity.Expires = session.Expires;
        entity.Ticket = session.Ticket;

        try
        {
            await Context.SaveChangesAsync(CancellationTokenProvider.CancellationToken);
            Logger.LogDebug("user session {userSessionKey} updated in database", session.Key);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Logger.LogWarning("exception updating existing user session {userSessionKey} in database: {error}", session.Key, ex.Message);
        }
    }

    /// <inheritdoc/>
    public virtual async Task DeleteUserSessionAsync(string key, CancellationToken cancellationToken = default)
    {
        var entity = (await Context.UserSessions.AsNoTracking().Where(x => x.Key == key)
                        .ToArrayAsync(CancellationTokenProvider.CancellationToken))
                    .SingleOrDefault(x => x.Key == key);

        if (entity == null)
        {
            Logger.LogDebug("no user session {userSessionKey} found in database. delete failed", key);
            return;
        }

        Context.UserSessions.Remove(entity);

        try
        {
            await Context.SaveChangesAsync(CancellationTokenProvider.CancellationToken);
            Logger.LogDebug("user session {userSessionKey} deleted in database", key);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Logger.LogWarning("exception deleting existing user session {userSessionKey} in database: {error}", key, ex.Message);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<GetAllUserSessionsResult> GetAllUserSessionsAsync(GetAllUserSessionsFilter filter = null, CancellationToken cancellationToken = default)
    {
        filter ??= new();
        if (filter.Page <= 0) filter.Page = 1;
        if (filter.Count <= 0) filter.Count = 25;

        var query = Context.UserSessions.AsQueryable();

        if (!String.IsNullOrWhiteSpace(filter.DisplayName) ||
            !String.IsNullOrWhiteSpace(filter.SubjectId) ||
            !String.IsNullOrWhiteSpace(filter.SessionId))
        {
            query = query.Where(x =>
                (filter.DisplayName == null || (x.DisplayName != null && x.DisplayName.Contains(filter.DisplayName) == true)) ||
                (filter.SubjectId == null || x.SubjectId.Contains(filter.SubjectId)) ||
                (filter.SessionId == null || x.SessionId.Contains(filter.SessionId))
            );
        }

        var totalCount = query.Count();
        var countRequested = filter.Count;

        var totalPages = (int) Math.Max(1, Math.Ceiling(totalCount / (countRequested * 1.0)));
        var currentPage = Math.Min(filter.Page, totalPages);

        var results = await query.Skip(currentPage - 1).Take(countRequested)
            .Select(entity => new UserSessionSummary
            {
                Key = entity.Key,
                Scheme = entity.Scheme,
                SubjectId = entity.SubjectId,
                SessionId = entity.SessionId,
                DisplayName = entity.DisplayName,
                Created = entity.Created,
                Renewed = entity.Renewed,
                Expires = entity.Expires,
            })
            .ToArrayAsync();

        var result = new GetAllUserSessionsResult
        {
            Page = currentPage,
            CountRequested = countRequested,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Results = results
        };

        Logger.LogDebug("user sessions found in the db {userSessionCount}", results.Length);

        return result;
    }

    /// <inheritdoc/>
    public virtual async Task<IReadOnlyCollection<UserSession>> GetUserSessionsAsync(UserSessionsFilter filter, CancellationToken cancellationToken = default)
    {
        filter.Validate();

        var entities = await Filter(Context.UserSessions.AsQueryable(), filter)
            .ToArrayAsync(CancellationTokenProvider.CancellationToken);
        entities = Filter(entities.AsQueryable(), filter).ToArray();

        var results = entities.Select(entity => new UserSession
        {
            Key = entity.Key,
            Scheme = entity.Scheme,
            SubjectId = entity.SubjectId,
            SessionId = entity.SessionId,
            DisplayName = entity.DisplayName,
            Created = entity.Created,
            Renewed = entity.Renewed,
            Expires = entity.Expires,
            Ticket = entity.Ticket,
        }).ToArray();

        Logger.LogDebug("{userSessionCount} user sessions found for {@filter}", results.Length, filter);

        return results;
    }

    /// <inheritdoc/>
    public virtual async Task DeleteUserSessionsAsync(UserSessionsFilter filter, CancellationToken cancellationToken = default)
    {
        filter.Validate();

        var entities = await Filter(Context.UserSessions.AsQueryable(), filter)
            .ToArrayAsync(CancellationTokenProvider.CancellationToken);
        entities = Filter(entities.AsQueryable(), filter).ToArray();

        Context.UserSessions.RemoveRange(entities);

        try
        {
            await Context.SaveChangesAsync(CancellationTokenProvider.CancellationToken);
            Logger.LogDebug("removed {userSessionCount} user sessions from database for {@filter}", entities.Length, filter);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Logger.LogInformation("error removing {userSessionCount} user sessions from database for {@filter}: {error}", entities.Length, filter, ex.Message);
        }
    }

    private IQueryable<Entities.UserSession> Filter(IQueryable<Entities.UserSession> query, UserSessionsFilter filter)
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
}