// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Http;
using System;

namespace Duende.IdentityServer.Configuration;

/// <summary>
/// Configures the behavior for backchannel logout support for upstream IdPs.
/// </summary>
public class BackchannelLogoutOptions
{
    /// <summary>
    /// Prefix in the pipeline for callbacks from external IdPs. Defaults to "/backchannel".
    /// </summary>
    public PathString PathPrefix { get; set; } = "/backchannel";
}
