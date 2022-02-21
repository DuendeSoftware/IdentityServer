// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Duende.SessionManagement;

/// <summary>
/// Custom type for ITicketStore
/// </summary>
// This is here really just to avoid possible confusion of any other ITicketStore already in the DI system.
public interface IServerSideTicketStore : ITicketStore 
{
    /// <summary>
    /// Gets sessions for a specific subject id and/or session id
    /// </summary>
    Task<IReadOnlyCollection<UserSession>> GetSessionsAsync(SessionFilter filter, CancellationToken cancellationToken = default);


    /// <summary>
    /// Queries user sessions based on filter
    /// </summary>
    Task<QueryResult<UserSession>> QuerySessionsAsync(QueryFilter? filter = null, CancellationToken cancellationToken = default);
}
