// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Stores;

/// <summary>
/// User session store
/// </summary>
public interface IServerSideSessionStore
{
    /// <summary>
    /// Retrieves a session
    /// </summary>
    Task<ServerSideSession> GetSessionAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a session
    /// </summary>
    Task CreateSessionAsync(ServerSideSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a session
    /// </summary>
    Task UpdateSessionAsync(ServerSideSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a session
    /// </summary>
    Task DeleteSessionAsync(string key, CancellationToken cancellationToken = default);


    /// <summary>
    /// Gets sessions for a specific subject id and/or session id
    /// </summary>
    Task<IReadOnlyCollection<ServerSideSession>> GetSessionsAsync(SessionFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes sessions for a specific subject id and/or session id
    /// </summary>
    Task DeleteSessionsAsync(SessionFilter filter, CancellationToken cancellationToken = default);



    /// <summary>
    /// Queries sessions based on filter
    /// </summary>
    Task<QueryResult<ServerSideSession>> QuerySessionsAsync(SessionQuery filter = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Filter to query user sessions
/// </summary>
public class SessionFilter
{
    /// <summary>
    /// The subject ID
    /// </summary>
    public string SubjectId { get; init; }

    /// <summary>
    /// The sesion ID
    /// </summary>
    public string SessionId { get; init; }

    /// <summary>
    /// Validates
    /// </summary>
    public void Validate()
    {
        if (String.IsNullOrWhiteSpace(SubjectId) && String.IsNullOrWhiteSpace(SessionId))
        {
            throw new ArgumentNullException("SubjectId or SessionId is required.");
        }
    }
}

/// <summary>
/// Filter to query all user sessions
/// </summary>
public class SessionQuery
{
    /// <summary>
    /// The page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// The number to return
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// The subject ID
    /// </summary>
    public string SubjectId { get; init; }

    /// <summary>
    /// The sesion ID
    /// </summary>
    public string SessionId { get; init; }

    /// <summary>
    /// The user display name
    /// </summary>
    public string DisplayName { get; init; }
}
