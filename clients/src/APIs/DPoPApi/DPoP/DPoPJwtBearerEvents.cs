using IdentityModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Linq;
using System.Threading.Tasks;
using static IdentityModel.OidcConstants;

namespace DPoPApi;

public class DPoPJwtBearerEvents : JwtBearerEvents
{
    private readonly DPoPProofValidator _validator;

    public DPoPJwtBearerEvents(DPoPProofValidator validator)
    {
        _validator = validator;
    }

    public override Task MessageReceived(MessageReceivedContext context)
    {
        var authz = context.HttpContext.Request.Headers.Authorization.FirstOrDefault();
        if (authz.StartsWith(OidcConstants.AuthenticationSchemes.AuthorizationHeaderDPoP + " "))
        {
            var value = authz.Substring((OidcConstants.AuthenticationSchemes.AuthorizationHeaderDPoP + " ").Length).Trim();
            context.Token = value;
        }
        else
        {
            // this rejects the attempt for this handler
            context.NoResult();
        }

        return Task.CompletedTask;
    }

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var dpopProofToken = context.HttpContext.Request.Headers[OidcConstants.HttpHeaders.DPoP].FirstOrDefault();

        var result = await _validator.ValidateAsync(new DPoPProofValidatonContext
        {
            ProofToken = dpopProofToken,
            AccessTokenClaims = context.Principal.Claims,
            Method = context.HttpContext.Request.Method,
            Url = context.HttpContext.Request.Scheme + "://" + context.HttpContext.Request.Host + context.HttpContext.Request.PathBase + context.HttpContext.Request.Path
        });

        if (result.IsError)
        {
            if (result.ServerIssuedNonce != null)
            {
                context.HttpContext.Items["DPoP-Nonce"] = result.ServerIssuedNonce;
            }
            context.HttpContext.Items["DPoP-Error"] = result.Error;
            // fails the result
            context.Fail(result.ErrorDescription);
        }
    }

    public override Task Challenge(JwtBearerChallengeContext context)
    {
        if (context.HttpContext.Items.ContainsKey("DPoP-Error"))
        {
            var error = context.HttpContext.Items["DPoP-Error"] as string;
            context.Error = error;
        }
        if (context.HttpContext.Items.ContainsKey("DPoP-Nonce"))
        {
            var nonce = context.HttpContext.Items["DPoP-Nonce"] as string;
            context.Response.Headers[HttpHeaders.DPoPNonce] = nonce;
        }
        return Task.CompletedTask;
    }
}
