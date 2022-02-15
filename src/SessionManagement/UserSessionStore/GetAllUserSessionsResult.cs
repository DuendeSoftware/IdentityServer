// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


namespace Duende.SessionManagement;

/// <summary>
/// Query result for all user sessions
/// </summary>
public class GetAllUserSessionsResult
{
    /// <summary>
    /// The page number
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// The number to return
    /// </summary>
    public int Count { get; init; }
    
    /// <summary>
    /// The total count
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// The results.
    /// </summary>
    public IReadOnlyCollection<UserSessionSummary> Results { get; init; } = default!;
}
