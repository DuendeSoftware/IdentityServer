﻿// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Duende.IdentityServer.Validation;

/// <summary>
/// Models the result of DPoP proof validation.
/// </summary>
public class DPoPProofValidatonResult : ValidationResult
{
    /// <summary>
    /// The serialized JWK from the validated DPoP proof token.
    /// </summary>
    public string JsonWebKey { get; set; }

    /// <summary>
    /// The JWK thumbprint from the validated DPoP proof token.
    /// </summary>
    public string JsonWebKeyThumbprint { get; set; }

    /// <summary>
    /// The payload value of the DPoP proof token.
    /// </summary>
    public IDictionary<string, object> Payload { get; internal set; }

    /// <summary>
    /// The jti value read from the payload.
    /// </summary>
    public string TokenId { get; set; }
    
    /// <summary>
    /// The nonce value read from the payload.
    /// </summary>
    public string Nonce { get; set; }

    /// <summary>
    /// The iat value read from the payload.
    /// </summary>
    public long? IssuedAt{ get; set; }

    /// <summary>
    /// The nonce value issued by the server.
    /// </summary>
    public string ServerIssuedNonce { get; set; }

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