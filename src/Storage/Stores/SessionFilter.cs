// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System;

namespace Duende.IdentityServer.Stores;

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
    /// The session ID
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
