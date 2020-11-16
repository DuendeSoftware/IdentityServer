using System.Threading.Tasks;
using IdentityModel;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace MvcCode
{
    public class OidcEvents : OpenIdConnectEvents
    {
        private readonly AssertionService _assertionService;

        public OidcEvents(AssertionService assertionService)
        {
            _assertionService = assertionService;
        }
        
        public override Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
        {
            context.TokenEndpointRequest.ClientAssertionType = OidcConstants.ClientAssertionTypes.JwtBearer;
            context.TokenEndpointRequest.ClientAssertion = _assertionService.CreateClientToken();

            return Task.CompletedTask;
        }

        public override Task RedirectToIdentityProvider(RedirectContext context)
        {
            var request = _assertionService.SignAuthorizationRequest(context.ProtocolMessage);
            var clientId = context.ProtocolMessage.ClientId;
            var redirectUri = context.ProtocolMessage.RedirectUri;
            
            context.ProtocolMessage.Parameters.Clear();
            context.ProtocolMessage.ClientId = clientId;
            context.ProtocolMessage.RedirectUri = redirectUri;
            context.ProtocolMessage.SetParameter("request", request);

            return Task.CompletedTask;
        }
    }
}