// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;

namespace Duende.IdentityServer.Models;

/// <summary>
/// Results from querying user sessions from session management service.
/// </summary>
public class UserSession
{
    /// <summary>
    /// The subject ID
    /// </summary>
    public string SubjectId { get; set; } = default!;

    /// <summary>
    /// The session ID
    /// </summary>
    public string SessionId { get; set; } = default!;

    /// <summary>
    /// The display name for the user
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// The creation time
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// The renewal time
    /// </summary>
    public DateTime Renewed { get; set; }

    /// <summary>
    /// The expiration time
    /// </summary>
    public DateTime? Expires { get; set; }

    /// <summary>
    /// The issuer of the token service at login time.
    /// </summary>
    public string Issuer { get; set; }

    /// <summary>
    /// The client ids for the session
    /// </summary>
    public IReadOnlyCollection<string> ClientIds { get; set; } = default!;

    /// <summary>
    /// The underlying AuthenticationTicket
    /// </summary>
    public AuthenticationTicket AuthenticationTicket { get; set; } = default!;
}

