// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;

namespace Duende.IdentityServer.Configuration;

/// <summary>
/// Options for DPoP
/// </summary>
public class DPoPOptions
{
    /// <summary>
    /// Default DPoP token validity duration. Defaults to 1 minute.
    /// </summary>
    public TimeSpan DPoPTokenValidityDuration { get; set; }
    
    ///// <summary>
    ///// Clock skew
    ///// </summary>
    //public TimeSpan ClockSkew { get; set; }
}