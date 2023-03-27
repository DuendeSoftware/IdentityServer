using IdentityModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Threading.Tasks;

namespace DPoPApi;

public class RequireCnfJwtBearerEvents : JwtBearerEvents
{
    public override Task TokenValidated(TokenValidatedContext context)
    {
        if (context.Principal.HasClaim(x => x.Type == JwtClaimTypes.Confirmation))
        {
            context.Fail("Must use DPoP when using a token with a 'cnf' claim");
        }

        return Task.CompletedTask;
    }
}
