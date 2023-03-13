using IdentityModel;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MvcDPoP;

public class DPoPProofApiMessageHandler : DelegatingHandler
{
    private IHttpContextAccessor _http;

    public DPoPProofApiMessageHandler(IHttpContextAccessor httpContextAccessor)
    {
        _http = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var proofKey = await _http.HttpContext?.GetProofKeyAsync();
        if (proofKey != null)
        {
            var proofToken = proofKey.CreateProofToken(request.Method.ToString(), request.RequestUri.ToString());
            request.Headers.Add(OidcConstants.HttpHeaders.DPoP, proofToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
