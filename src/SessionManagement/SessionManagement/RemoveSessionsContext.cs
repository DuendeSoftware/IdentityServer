// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

namespace Duende.SessionManagement;

/// <summary>
/// Models the information to remove a user's session data.
/// </summary>
public class RemoveSessionsContext
{
    /// <summary>
    /// The subject ID
    /// </summary>
    public string? SubjectId { get; init; }

    /// <summary>
    /// The sesion ID
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// The client ids for which to trigger logout notification, or revoke tokens or consent.
    /// If not set, then all clients will be removed.
    /// </summary>
    public IEnumerable<string> ClientIds { get; set; } = default!;

    /// <summary>
    /// Removes the server side session cookie for the user's session.
    /// </summary>
    public bool RemoveServerSideSessionCookie { get; set; } = true;

    /// <summary>
    /// Sends a back channel logout notification (if clients are registered for one).
    /// </summary>
    public bool SendBackchannelLogoutNotification { get; set; } = true;
    
    /// <summary>
    /// Revokes all tokens (refresh and reference) for the clients.
    /// </summary>
    public bool RevokeTokens { get; set; } = true;

    /// <summary>
    /// Revokes all prior consent granted to the clients.
    /// </summary>
    public bool RevokeConsents { get; set; } = true;
}
