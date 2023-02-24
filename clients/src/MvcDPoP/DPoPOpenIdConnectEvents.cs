using Clients;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace MvcDPoP;

public class DPoPOpenIdConnectEvents : OpenIdConnectEvents
{
    public override Task RedirectToIdentityProvider(RedirectContext context)
    {
        var key = CreateDPoPProofKey();
        context.Properties.Items["dpop_jwks"] = JsonSerializer.Serialize(key);

        var dpop_jkt = Base64UrlEncoder.Encode(key.ComputeJwkThumbprint());
        context.ProtocolMessage.Parameters["dpop_jkt"] = dpop_jkt;

        return base.RedirectToIdentityProvider(context);
    }

    public override async Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
    {
        var dpop_jwks = context.Properties.Items["dpop_jwks"];
        var key = new JsonWebKey(dpop_jwks);
        
        var proofToken = CreateProofToken(key);
        context.HttpContext.Items["dpop_proof_token"] = proofToken;

        await base.AuthorizationCodeReceived(context);
    }

    string CreateProofToken(JsonWebKey key)
    {
        var payload = new Dictionary<string, object>
        {
            { "jti", Guid.NewGuid().ToString() },
            { "htm", "POST" },
            { "htu", $"{Constants.Authority}/connect/token" },
            { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
        };

        var header = new Dictionary<string, object>()
        {
            //{ "alg", "RS265" }, // JsonWebTokenHandler requires adding this itself
            { 
                "typ", "dpop+jwk" 
            },
            { 
                "jwk", new 
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

    JsonWebKey CreateDPoPProofKey()
    {
        var rsaKey = new RsaSecurityKey(RSA.Create(2048));

        var jwk = JsonWebKeyConverter.ConvertFromSecurityKey(rsaKey);
        jwk.Alg = "RS256";

        return jwk;
    }
}
