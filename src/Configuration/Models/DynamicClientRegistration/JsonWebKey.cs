//------------------------------------------------------------------------------
//
// Review - Unclear what the copyright of this code is
// Review - I've copied this from IdentityModel so that I can change the json
// serialization. I think I need a proper model 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using IdentityModel.Jwk;
using IdentityModel;

namespace Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;

/// <summary>
/// Represents a Json Web Key as defined in http://tools.ietf.org/html/rfc7517.
/// </summary>
public class DCR_JsonWebKey
{
    // kept private to hide that a List is used.
    // public member returns an IList.
    private IList<string>? _certificateClauses;
    private IList<string>? _keyops;

    /// <summary>
    /// Initializes an new instance of <see cref="DCR_JsonWebKey"/>.
    /// </summary>
    public DCR_JsonWebKey()
    {
    }

    /// <summary>
    /// Initializes an new instance of <see cref="DCR_JsonWebKey"/> from a json string.
    /// </summary>
    /// <param name="json">a string that contains JSON Web Key parameters in JSON format.</param>
    public DCR_JsonWebKey(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentNullException(nameof(json));

        var key = JsonSerializer.Deserialize<DCR_JsonWebKey>(json);
        if (key == null) throw new InvalidOperationException("malformed key");

        Copy(key);
    }

    private void Copy(DCR_JsonWebKey key)
    {
        this.Alg = key.Alg;
        this.Crv = key.Crv;
        this.D = key.D;
        this.DP = key.DP;
        this.DQ = key.DQ;
        this.E = key.E;
        this.K = key.K;
        this.Kid = key.Kid;
        this.Kty = key.Kty;
        this.N = key.N;
        this.Oth = key.Oth;
        this.P = key.P;
        this.Q = key.Q;
        this.QI = key.QI;
        this.Use = key.Use;
        this.X5t = key.X5t;
        this.X5tS256 = key.X5tS256;
        this.X5u = key.X5u;
        this.X = key.X;
        this.Y = key.Y;

        this._certificateClauses = key.X5c is null ? null : new List<string>(key.X5c);
        this._keyops = key.KeyOps is null ? null : new List<string>(key.KeyOps);
    }

    /// <summary>
    /// Gets or sets the 'alg' (KeyType)..
    /// </summary>
    [JsonPropertyName(JsonWebKeyParameterNames.Alg)]
    public string? Alg { get; set; }

    /// <summary>
    /// Gets or sets the 'crv' (ECC - Curve)..
    /// </summary>
    [JsonPropertyName(JsonWebKeyParameterNames.Crv)]
    public string? Crv { get; set; }

    /// <summary>
    /// Gets or sets the 'd' (ECC - Private Key OR RSA - Private Exponent)..
    /// </summary>
    /// <remarks> value is formated as: Base64urlUInt</remarks>
    [JsonPropertyName(JsonWebKeyParameterNames.D)]
    public string? D { get; set; }

    /// <summary>
    /// Gets or sets the 'dp' (RSA - First Factor CRT Exponent)..
    /// </summary>
    /// <remarks> value is formated as: Base64urlUInt</remarks>
    [JsonPropertyName(JsonWebKeyParameterNames.DP)]
    public string? DP { get; set; }

    /// <summary>
    /// Gets or sets the 'dq' (RSA - Second Factor CRT Exponent)..
    /// </summary>
    /// <remarks> value is formated as: Base64urlUInt</remarks>
    [JsonPropertyName(JsonWebKeyParameterNames.DQ)]
    public string? DQ { get; set; }

    /// <summary>
    /// Gets or sets the 'e' (RSA - Exponent)..
    /// </summary>
    [JsonPropertyName(JsonWebKeyParameterNames.E)]
    public string? E { get; set; }

    /// <summary>
    /// Gets or sets the 'k' (Symmetric - Key Value)..
    /// </summary>
    /// Base64urlEncoding
    [JsonPropertyName(JsonWebKeyParameterNames.K)]
    public string? K { get; set; }

    /// <summary>
    /// Gets or sets the 'key_ops' (Key Operations)..
    /// </summary>
    [JsonPropertyName(JsonWebKeyParameterNames.KeyOps)]
    public IList<string>? KeyOps
    {
        get { return _keyops; }
        set
        {
            if (value is not null)
            {
                _keyops ??= new List<string>();
                foreach (string keyOp in value)
                    _keyops.Add(keyOp);
            }
        }
    }

    /// <summary>
    /// Gets or sets the 'kid' (Key ID)..
    /// </summary>
    [JsonPropertyName(JsonWebKeyParameterNames.Kid)]
    public string? Kid { get; set; }

    /// <summary>
    /// Gets or sets the 'kty' (Key Type)..
    /// </summary>
    [JsonPropertyName(JsonWebKeyParameterNames.Kty)]
    public string? Kty { get; set; }

    /// <summary>
    /// Gets or sets the 'n' (RSA - Modulus)..
    /// </summary>
    /// <remarks> value is formated as: Base64urlEncoding</remarks>
    [JsonPropertyName(JsonWebKeyParameterNames.N)]
    public string? N { get; set; }

    /// <summary>
    /// Gets or sets the 'oth' (RSA - Other Primes Info)..
    /// </summary>
    [JsonPropertyName(JsonWebKeyParameterNames.Oth)]
    public IList<string>? Oth { get; set; }

    /// <summary>
    /// Gets or sets the 'p' (RSA - First Prime Factor)..
    /// </summary>
    /// <remarks> value is formated as: Base64urlUInt</remarks>
    [JsonPropertyName(JsonWebKeyParameterNames.P)]
    public string? P { get; set; }

    /// <summary>
    /// Gets or sets the 'q' (RSA - Second  Prime Factor)..
    /// </summary>
    /// <remarks> value is formated as: Base64urlUInt</remarks>
    [JsonPropertyName(JsonWebKeyParameterNames.Q)]
    public string? Q { get; set; }

    /// <summary>
    /// Gets or sets the 'qi' (RSA - First CRT Coefficient)..
    /// </summary>
    /// <remarks> value is formated as: Base64urlUInt</remarks>
    [JsonPropertyName(JsonWebKeyParameterNames.QI)]
    public string? QI { get; set; }

    /// <summary>
    /// Gets or sets the 'use' (Public Key Use)..
    /// </summary>
    [JsonPropertyName(JsonWebKeyParameterNames.Use)]
    public string? Use { get; set; }

    /// <summary>
    /// Gets or sets the 'x' (ECC - X Coordinate)..
    /// </summary>
    /// <remarks> value is formated as: Base64urlEncoding</remarks>
    [JsonPropertyName(JsonWebKeyParameterNames.X)]
    public string? X { get; set; }

    /// <summary>
    /// Gets the 'x5c' collection (X.509 Certificate Chain)..
    /// </summary>
    [JsonPropertyName(JsonWebKeyParameterNames.X5c)]
    public IList<string>? X5c
    {
        get { return _certificateClauses; }
        set
        {
            if (value is not null)
            {
                _certificateClauses ??= new List<string>();
                foreach (string clause in value)
                    _certificateClauses.Add(clause);
            }
        }
    }

    /// <summary>
    /// Gets or sets the 'x5t' (X.509 Certificate SHA-1 thumbprint)..
    /// </summary>
    [JsonPropertyName(JsonWebKeyParameterNames.X5t)]
    public string? X5t { get; set; }

    /// <summary>
    /// Gets or sets the 'x5t#S256' (X.509 Certificate SHA-1 thumbprint)..
    /// </summary>
    [JsonPropertyName(JsonWebKeyParameterNames.X5tS256)]
    public string? X5tS256 { get; set; }

    /// <summary>
    /// Gets or sets the 'x5u' (X.509 URL)..
    /// </summary>
    [JsonPropertyName(JsonWebKeyParameterNames.X5u)]
    public string? X5u { get; set; }

    /// <summary>
    /// Gets or sets the 'y' (ECC - Y Coordinate)..
    /// </summary>
    /// <remarks> value is formated as: Base64urlEncoding</remarks>
    [JsonPropertyName(JsonWebKeyParameterNames.Y)]
    public string? Y { get; set; }

    [JsonIgnore]
    public int KeySize
    {
        get
        {
            if (Kty == JsonWebAlgorithmsKeyTypes.RSA)
                return Base64Url.Decode(N!).Length * 8;
            else if (Kty == JsonWebAlgorithmsKeyTypes.EllipticCurve)
                return Base64Url.Decode(X!).Length * 8;
            else if (Kty == JsonWebAlgorithmsKeyTypes.Octet)
                return Base64Url.Decode(K!).Length * 8;
            else
                return 0;
        }
    }

    [JsonIgnore]
    public bool HasPrivateKey
    {
        get
        {
            if (Kty == JsonWebAlgorithmsKeyTypes.RSA)
                return D != null && DP != null && DQ != null && P != null && Q != null && QI != null;
            else if (Kty == JsonWebAlgorithmsKeyTypes.EllipticCurve)
                return D != null;
            else
                return false;
        }
    }
}