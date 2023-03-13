using Clients;
using IdentityModel;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Threading.Tasks;

namespace MvcDPoP;

public class DPoPOpenIdConnectEvents : OpenIdConnectEvents
{
    public override Task RedirectToIdentityProvider(RedirectContext context)
    {
        // create the dpop key
        var key = DPoPProof.CreateProofKey();
        
        // we store the proof key here to avoid server side and load balancing storage issues
        context.Properties.SetProofKey(key);
        
        // pass jkt to authorize endpoint
        context.ProtocolMessage.Parameters[OidcConstants.AuthorizeRequest.DPoPKeyThumbprint] = key.CreateJkt();

        return base.RedirectToIdentityProvider(context);
    }

    public override async Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
    {
        // get key from storage
        var key = context.Properties.GetProofKey();

        // create proof token for token endpoint
        var proofToken = key.CreateProofToken("POST", $"{Constants.Authority}/connect/token");

        // set it so the OIDC message handler can find it
        context.HttpContext.SetOutboundProofToken(proofToken);

        await base.AuthorizationCodeReceived(context);
    }

}
