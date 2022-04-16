// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Security.Claims;

namespace Duende.IdentityServer.Configuration.WebApi.v1;

/// <summary>
/// A client claim
/// </summary>
public class ClientClaim
{
    /// <summary>
    /// The claim type
    /// </summary>
    public string Type { get; set; }
        
    /// <summary>
    /// The claim value
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// The claim value type
    /// </summary>
    public string ValueType { get; set; } = ClaimValueTypes.String;
}