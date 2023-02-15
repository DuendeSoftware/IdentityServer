// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation;

/// <summary>
/// Validator for handling DPoP proofs.
/// </summary>
public interface IDPoPProofValidator
{
    /// <summary>
    /// Validates the DPoP proof.
    /// </summary>
    Task<DPoPProofValidatonResult> ValidateAsync(DPoPProofValidatonContext context);
}

/// <summary>
/// 
/// </summary>
public class DPoPProofValidatonContext
{
    /// <summary>
    /// The DPoP proof token to validate.
    /// </summary>
    public string ProofToken { get; internal set; }
}

/// <summary>
/// Models the result of DPoP proof validation.
/// </summary>
public class DPoPProofValidatonResult : ValidationResult
{
    /// <summary>
    /// The JWK thumbprint from the validated DPoP proof token.
    /// </summary>
    public string JsonWebKeyThumbprint { get; set; }

    /// <summary>
    /// The payload of the DPoP proof token.
    /// </summary>
    public IDictionary<string, object> ProofPayload { get; internal set; }

    /// <summary>
    /// The serialized JWK from the validated DPoP proof token.
    /// </summary>
    public string JsonWebKey { get; set; }

    /// <summary>
    /// Create the "cnf" value from the JsonWebKeyThumbprint value;
    /// </summary>
    public string CreateThumbprintCnf()
    {
        if (String.IsNullOrWhiteSpace(JsonWebKeyThumbprint)) return String.Empty;

        var values = new Dictionary<string, string>
        {
            // TODO: IdentityModel
            { "jkt", JsonWebKeyThumbprint }
        };
        return JsonSerializer.Serialize(values);
    }
}
