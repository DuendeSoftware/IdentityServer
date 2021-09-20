using Microsoft.AspNetCore.Http;
using System.Threading;

namespace Duende.IdentityServer.Services.Default
{
    class DefaultHttpContextCancellationTokenService : ICancellationTokenService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DefaultHttpContextCancellationTokenService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public CancellationToken CancellationToken => _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
    }
}
