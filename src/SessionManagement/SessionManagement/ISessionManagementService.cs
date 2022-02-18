// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

namespace Duende.SessionManagement;

/// <summary>
/// Session management service
/// </summary>
public interface ISessionManagementService
{
    /// <summary>
    /// Removes all the session related data for a user.
    /// </summary>
    Task<QueryResult<UserSession>> QuerySessionsAsync(QueryFilter? filter = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes all the session related data for a user.
    /// </summary>
    Task RemoveSessionsAsync(RemoveSessionsContext context, CancellationToken cancellationToken = default);
}
