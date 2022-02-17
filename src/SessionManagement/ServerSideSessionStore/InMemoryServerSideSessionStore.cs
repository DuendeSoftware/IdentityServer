// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Collections.Concurrent;

namespace Duende.SessionManagement;

/// <summary>
/// In-memory user session store
/// </summary>
public class InMemoryServerSideSessionStore : IServerSideSessionStore
{
    private readonly ConcurrentDictionary<string, ServerSideSession> _store = new();



    /// <inheritdoc />
    public Task CreateSessionAsync(ServerSideSession session, CancellationToken cancellationToken = default)
    {
        if (!_store.TryAdd(session.Key, session.Clone()))
        {
            throw new Exception("Key already exists");
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ServerSideSession?> GetSessionAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(key, out var item);
        return Task.FromResult(item?.Clone());
    }

    /// <inheritdoc />
    public Task UpdateSessionAsync(ServerSideSession session, CancellationToken cancellationToken = default)
    {
        _store[session.Key] = session.Clone();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteSessionAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }



    /// <inheritdoc />
    public Task<IReadOnlyCollection<ServerSideSession>> GetSessionsAsync(SessionFilter filter, CancellationToken cancellationToken = default)
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
        return Task.FromResult((IReadOnlyCollection<ServerSideSession>) results);
    }

    /// <inheritdoc />
    public Task DeleteSessionsAsync(SessionFilter filter, CancellationToken cancellationToken = default)
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
    public Task<QuerySessionsResult> QuerySessionsAsync(QueryFilter? filter = null, CancellationToken cancellationToken = default)
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
        
        var totalCount = query.Count();
        var countRequested = filter.Count;

        var totalPages = (int) Math.Max(1, Math.Ceiling(totalCount / (countRequested * 1.0)));
        var currentPage = Math.Min(filter.Page, totalPages);

        var results = query.Skip(currentPage - 1).Take(countRequested)
            .Select(x => x.Clone()).ToArray();

        var result = new QuerySessionsResult
        { 
            Page = currentPage,
            CountRequested = countRequested,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Results = results
        };

        return Task<QuerySessionsResult>.FromResult(result);
    }
}
