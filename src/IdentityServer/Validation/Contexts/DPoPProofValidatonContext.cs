// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Validation;

/// <summary>
/// Models the context for validaing DPoP proof tokens.
/// </summary>
public class DPoPProofValidatonContext
{
    /// <summary>
    /// The client presenting the DPoP proof
    /// </summary>
    public Client Client { get; set; }

    /// <summary>
    /// The DPoP proof token to validate
    /// </summary>
    public string ProofToken { get; set; }
}
