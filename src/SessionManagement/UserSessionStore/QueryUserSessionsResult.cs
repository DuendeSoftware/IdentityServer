// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


namespace Duende.SessionManagement;

/// <summary>
/// Query result for all user sessions
/// </summary>
public class QueryUserSessionsResult
{
    /// <summary>
    /// The page number requested
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// The number to return
    /// </summary>
    public int CountRequested { get; init; }

    /// <summary>
    /// The total count
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// The total pages
    /// </summary>
    public int TotalPages { get; init; }

    /// <summary>
    /// The results.
    /// </summary>
    public IReadOnlyCollection<UserSession> Results { get; init; } = default!;
}
