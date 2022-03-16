// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;

namespace Duende.IdentityServer.Configuration;

/// <summary>
/// Configures the behavior for server-side sessions.
/// </summary>
public class ServerSideSessionOptions
{
    /// <summary>
    /// The claim type used for the user's display name.
    /// </summary>
    public string UserDisplayNameClaimType { get; set; }

    /// <summary>
    /// Controls if server-side session expiration is extended when refresh tokens are used.
    /// </summary>
    public bool ExtendSessionExpirationOnRefreshTokenUse { get; set; }
    
    /// <summary>
    /// Controls if when server-side sessions expire if back-channel logout notifications are sent.
    /// </summary>
    public bool ExpiredSessionsTriggerBackchannelLogout { get; set; }

    /// <summary>
    /// If enabled will perodically cleanup expired sessions.
    /// </summary>
    public bool RemoveExpiredSessions { get; set; } = true;

    /// <summary>
    /// Frequency expired sessions will be removed.
    /// </summary>
    public TimeSpan RemoveExpiredSessionsFrequency { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Number of expired sessions records to be removed at a time.
    /// </summary>
    public int RemoveExpiredSessionsBatchSize { get; set; } = 100;
}
