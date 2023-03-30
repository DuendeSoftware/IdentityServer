using IdentityModel;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Text.Json;

namespace DPoPApi;

/// <summary>
/// Extensions methods for JsonWebKey
/// </summary>
static class JsonWebKeyExtensions
{
    /// <summary>
    /// Create the value of a thumbprint-based cnf claim
    /// </summary>
    public static string CreateThumbprintCnf(this JsonWebKey jwk)
    {
        var jkt = jwk.CreateThumbprint();
        var values = new Dictionary<string, string>
        {
            { JwtClaimTypes.ConfirmationMethods.JwkThumbprint, jkt }
        };
        return JsonSerializer.Serialize(values);
    }
    /// <summary>
    /// Create the value of a thumbprint
    /// </summary>
    public static string CreateThumbprint(this JsonWebKey jwk)
    {
        var jkt = Base64Url.Encode(jwk.ComputeJwkThumbprint());
        return jkt;
    }
}
