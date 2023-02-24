using Clients;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MvcDPoP;

public class DPoPUserTokenEndpointService : IUserTokenEndpointService
{
    private readonly IHttpContextAccessor _http;
    private readonly UserTokenEndpointService _inner;

    public DPoPUserTokenEndpointService(IHttpContextAccessor httpContextAccessor, UserTokenEndpointService inner) 
    {
        _http = httpContextAccessor;
        _inner = inner;
    }

    public async Task<UserToken> RefreshAccessTokenAsync(string refreshToken, UserTokenRequestParameters parameters, CancellationToken cancellationToken = default)
    {
        // get dpop key from session
        var key = await _http.HttpContext.GetProofKey();
        
        // create proof token for token endpoint
        var proofToken = key.CreateProofToken("POST", $"{Constants.Authority}/connect/token");
        _http.HttpContext.SetOutboundProofToken(proofToken);
        
        return await _inner.RefreshAccessTokenAsync(refreshToken, parameters, cancellationToken);
    }

    public Task RevokeRefreshTokenAsync(string refreshToken, UserTokenRequestParameters parameters, CancellationToken cancellationToken = default)
    {
        return _inner.RevokeRefreshTokenAsync(refreshToken, parameters, cancellationToken);
    }
}
