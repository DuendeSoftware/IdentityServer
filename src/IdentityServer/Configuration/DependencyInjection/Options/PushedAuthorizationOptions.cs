// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Configuration.DependencyInjection.Options;

/// <summary>
/// The Pushed Authorization Options.
/// </summary>
public class PushedAuthorizationOptions
{
    /// <summary>
    /// Specifies whether pushed authorization requests are globally required.
    /// </summary>
    /// <remarks>
    /// There is also a per-client configuration flag in the Client
    /// configuration. Pushed authorization is required for a client if either
    /// this global configuration flag is enabled or if the flag is set for that
    /// client.
    /// </remarks>
    public bool Required { get; set; }

    /// <summary>
    /// Lifetime of pushed authorization requests in seconds.
    ///
    /// The pushed authorization request's lifetime begins when the request to
    /// the PAR endpoint is received, and is validated until the authorize
    /// endpoint returns a response to the client application. Note that user
    /// interaction, such as entering credentials or granting consent, may need
    /// to occur before the authorize endpoint can do so. Setting the lifetime
    /// too low will likely cause login failures for interactive users, if
    /// pushed authorization requests expire before those users complete
    /// authentication. Some security profiles, such as the FAPI 2.0 Security
    /// Profile recommend an expiration within 10 minutes to prevent attackers
    /// from pre-generating requests. To balance these constraints, the Lifetime
    /// defaults to 10 minutes.
    /// </summary>
    /// <remarks>There is also a per-client configuration setting that takes
    /// precedence over this global configuration.
    /// </remarks>
    public int Lifetime { get; set; } = 60*10;
}

