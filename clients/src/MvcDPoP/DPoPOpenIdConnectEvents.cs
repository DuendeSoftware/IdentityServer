using Clients;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Threading.Tasks;

namespace MvcDPoP;

public class DPoPOpenIdConnectEvents : OpenIdConnectEvents
{
    public override Task RedirectToIdentityProvider(RedirectContext context)
    {
        // create and store the dpop key
        var key = DPoPProof.CreateProofKey();
        context.Properties.SetProofKey(key);
        
        // pass jkt to authorize endpoint
        context.ProtocolMessage.Parameters["dpop_jkt"] = key.CreateJkt();

        return base.RedirectToIdentityProvider(context);
    }

    public override async Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
    {
        // get key from store
        var key = context.Properties.GetProofKey();

        // create proof token for token endpoint
        var proofToken = key.CreateProofToken("POST", $"{Constants.Authority}/connect/token");

        // set it so the OIDC message handler can find it
        context.HttpContext.SetOutboundProofToken(proofToken);

        await base.AuthorizationCodeReceived(context);
    }

}
