using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MvcDPoP;

public class DPoPProofTokenMessageHandler : DelegatingHandler
{
    private HttpContextAccessor _http;

    public DPoPProofTokenMessageHandler() : base(new HttpClientHandler())
    {
        _http = new HttpContextAccessor();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_http.HttpContext?.Items.TryGetValue("dpop_proof_token", out var dpopToken) is true)
        {
            request.Headers.Add("DPoP", dpopToken.ToString());
        }

        return base.SendAsync(request, cancellationToken);
    }
}
