// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

namespace Duende.SessionManagement;

/// <summary>
/// User session store
/// </summary>
public interface IServerSideSessionStore
{
    /// <summary>
    /// Retrieves a session
    /// </summary>
    /// <param name="key"></param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the asynchronous operation.</param>
    /// <returns></returns>
    Task<ServerSideSession?> GetSessionAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a session
    /// </summary>
    /// <param name="session"></param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the asynchronous operation.</param>
    /// <returns></returns>
    Task CreateSessionAsync(ServerSideSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a session
    /// </summary>
    /// <param name="session"></param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the asynchronous operation.</param>
    /// <returns></returns>
    Task UpdateSessionAsync(ServerSideSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a session
    /// </summary>
    /// <param name="key"></param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the asynchronous operation.</param>
    /// <returns></returns>
    Task DeleteSessionAsync(string key, CancellationToken cancellationToken = default);


    // todo: do we need these 2 methods?
    // BFF needed them, IIRC, for backchannel SLO
    /// <summary>
    /// Gets sessions for a specific subject id and/or session id
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the asynchronous operation.</param>
    /// <returns></returns>
    Task<IReadOnlyCollection<ServerSideSession>> GetSessionsAsync(SessionFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes sessions for a specific subject id and/or session id
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the asynchronous operation.</param>
    /// <returns></returns>
    Task DeleteSessionsAsync(SessionFilter filter, CancellationToken cancellationToken = default);



    /// <summary>
    /// Queries sessions based on filter
    /// </summary>
    /// <returns></returns>
    Task<QueryResult<ServerSideSession>> QuerySessionsAsync(QueryFilter? filter = null, CancellationToken cancellationToken = default);
}
