using IdentityModel;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MvcDPoP;

public class DPoPProofTokenEndpointMessageHandler : DelegatingHandler
{
    private HttpContextAccessor _http;

    public DPoPProofTokenEndpointMessageHandler() : base(new HttpClientHandler())
    {
        // unfortunate work around for this being designed as the
        // BackchannelHttpHandler property on the OpenIdConnectOptions
        _http = new HttpContextAccessor();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var proofToken = _http.HttpContext?.GetOutboundProofToken();
        if (proofToken != null)
        {
            request.Headers.Add(OidcConstants.HttpHeaders.DPoP, proofToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
