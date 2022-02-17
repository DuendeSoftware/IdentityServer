// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


namespace Duende.SessionManagement;

/// <summary>
/// Filter to query all user sessions
/// </summary>
public class QueryUserSessionsFilter
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
    public string? SubjectId { get; init; }

    /// <summary>
    /// The sesion ID
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// The user display name
    /// </summary>
    public string? DisplayName { get; init; }
}
