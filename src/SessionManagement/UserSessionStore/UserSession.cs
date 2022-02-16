// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

namespace Duende.SessionManagement;

/// <summary>
/// A user session
/// </summary>
public class UserSession : UserSessionSummary
{
    /// <summary>
    /// The serialized ticket
    /// </summary>
    public string Ticket { get; set; } = default!;

    /// <summary>
    /// Clones the instance
    /// </summary>
    /// <returns></returns>
    internal UserSession Clone()
    {
        var item = new UserSession()
        {
            Key = Key,
            Scheme = Scheme,
            SubjectId = SubjectId,
            SessionId = SessionId,
            DisplayName = DisplayName,
            Created = Created,
            Renewed = Renewed,
            Expires = Expires,
            Ticket = Ticket,
        };
        return item;
    }
}
