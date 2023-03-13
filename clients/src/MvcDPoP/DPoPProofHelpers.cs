using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace MvcDPoP;

public static class DPoPProof
{
    public static JsonWebKey CreateProofKey()
    {
        var rsaKey = new RsaSecurityKey(RSA.Create(2048));

        var jwk = JsonWebKeyConverter.ConvertFromSecurityKey(rsaKey);
        jwk.Alg = "RS256";

        return jwk;
    }

    public static string CreateProofToken(this JsonWebKey key, string method, string url)
    {
        var payload = new Dictionary<string, object>
        {
            { JwtClaimTypes.JwtId, Guid.NewGuid().ToString() },
            { JwtClaimTypes.DPoPHttpMethod, method },
            { JwtClaimTypes.DPoPHttpUrl, url },
            { JwtClaimTypes.IssuedAt, DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
        };

        var header = new Dictionary<string, object>()
        {
            //{ "alg", "RS265" }, // JsonWebTokenHandler requires adding this itself
            {
                "typ", JwtClaimTypes.JwtTypes.DPoPProofToken
            },
            {
                JwtClaimTypes.JsonWebKey, new
                       {
                         kty = key.Kty,
                         e = key.E,
                         n = key.N
                       }
            },
        };

        var handler = new JsonWebTokenHandler() { SetDefaultTimesOnTokenCreation = false };
        var token = handler.CreateToken(JsonSerializer.Serialize(payload), new SigningCredentials(key, "RS256"), header);
        return token;
    }

    public static string CreateJkt(this JsonWebKey key)
    {
        var dpop_jkt = Base64UrlEncoder.Encode(key.ComputeJwkThumbprint());
        return dpop_jkt;
    }

    public static void SetProofKey(this AuthenticationProperties properties, JsonWebKey key)
    {
        properties.Items["dpop_jwks"] = JsonSerializer.Serialize(key);
    }
    public static JsonWebKey GetProofKey(this AuthenticationProperties properties)
    {
        var dpop_jwks = properties.Items["dpop_jwks"];
        var key = new JsonWebKey(dpop_jwks);
        return key;
    }
    public static async Task<JsonWebKey> GetProofKeyAsync(this HttpContext context)
    {
        var authn = await context.AuthenticateAsync();
        return authn.Properties.GetProofKey();
    }

    public static void SetOutboundProofToken(this HttpContext context, string token)
    {
        context.Items["dpop_proof_token"] = token;
    }
    public static string GetOutboundProofToken(this HttpContext context)
    {
        var token = context.Items["dpop_proof_token"] as string;
        return token;
    }

    public static async Task CreateOutboundProofTokenAsync(this HttpContext context, string method, string url)
    {
        // get dpop key from session
        var key = await context.GetProofKeyAsync();

        // create proof token for token endpoint
        var proofToken = key.CreateProofToken(method, url);
        context.SetOutboundProofToken(proofToken);
    }
}
