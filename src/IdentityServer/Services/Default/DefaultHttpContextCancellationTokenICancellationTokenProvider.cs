using Microsoft.AspNetCore.Http;
using System.Threading;

namespace Duende.IdentityServer.Services.Default;

class DefaultHttpContextCancellationTokenICancellationTokenProvider : ICancellationTokenProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DefaultHttpContextCancellationTokenICancellationTokenProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CancellationToken CancellationToken => _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
}