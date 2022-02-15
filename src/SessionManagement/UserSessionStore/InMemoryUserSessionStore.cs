// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Collections.Concurrent;

namespace Duende.SessionManagement;

/// <summary>
/// In-memory user session store
/// </summary>
public class InMemoryUserSessionStore : IUserSessionStore
{
    private readonly ConcurrentDictionary<string, UserSession> _store = new();

    /// <inheritdoc />
    public Task CreateUserSessionAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        if (!_store.TryAdd(session.Key, session.Clone()))
        {
            throw new Exception("Key already exists");
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<UserSession?> GetUserSessionAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(key, out var item);
        return Task.FromResult(item?.Clone());
    }

    /// <inheritdoc />
    public Task UpdateUserSessionAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        _store[session.Key] = session.Clone();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteUserSessionAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<UserSession>> GetUserSessionsAsync(UserSessionsFilter filter, CancellationToken cancellationToken = default)
    {
        filter.Validate();

        var query = _store.Values.AsQueryable();
        if (!String.IsNullOrWhiteSpace(filter.SubjectId))
        {
            query = query.Where(x => x.SubjectId == filter.SubjectId);
        }
        if (!String.IsNullOrWhiteSpace(filter.SessionId))
        {
            query = query.Where(x => x.SessionId == filter.SessionId);
        }

        var results = query.Select(x => x.Clone()).ToArray();
        return Task.FromResult((IReadOnlyCollection<UserSession>) results);
    }

    /// <inheritdoc />
    public Task DeleteUserSessionsAsync(UserSessionsFilter filter, CancellationToken cancellationToken = default)
    {
        filter.Validate();

        var query = _store.Values.AsQueryable();
        if (!String.IsNullOrWhiteSpace(filter.SubjectId))
        {
            query = query.Where(x => x.SubjectId == filter.SubjectId);
        }
        if (!String.IsNullOrWhiteSpace(filter.SessionId))
        {
            query = query.Where(x => x.SessionId == filter.SessionId);
        }

        var keys = query.Select(x => x.Key).ToArray();

        foreach (var key in keys)
        {
            _store.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<GetAllUserSessionsResult> GetAllUserSessionsAsync(GetAllUserSessionsFilter? filter = null, CancellationToken cancellationToken = default)
    {
        filter ??= new();
        if (filter.Page <= 0) filter.Page = 1;
        if (filter.Count <= 0) filter.Count = 25;

        var query = _store.Values.AsQueryable();
        
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
        
        var count = query.Count();
        var results = query.Skip(filter.Page - 1).Take(filter.Count).ToArray();

        var result = new GetAllUserSessionsResult
        { 
            Page = filter.Page,
            Count = filter.Count,
            TotalCount = count,
            Results = results
        };

        return Task<GetAllUserSessionsResult>.FromResult(result);
    }
}
