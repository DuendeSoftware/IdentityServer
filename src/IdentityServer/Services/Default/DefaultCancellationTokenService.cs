using Microsoft.AspNetCore.Http;
using System.Threading;

namespace Duende.IdentityServer.Services.Default
{
    class DefaultCancellationTokenService : ICancellationTokenService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DefaultCancellationTokenService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public CancellationToken CancellationToken => _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
    }
}
