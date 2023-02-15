// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

namespace Duende.IdentityServer.Validation;

/// <summary>
/// Models the context for validaing DPoP proof tokens.
/// </summary>
public class DPoPProofValidatonContext
{
    /// <summary>
    /// The DPoP proof token to validate.
    /// </summary>
    public string ProofToken { get; internal set; }
}
