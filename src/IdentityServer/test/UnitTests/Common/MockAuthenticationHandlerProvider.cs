using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace IdentityServer.UnitTests.Common
{
    internal class MockAuthenticationHandlerProvider : IAuthenticationHandlerProvider
    {
        public IAuthenticationHandler Handler { get; set; }

        public Task<IAuthenticationHandler> GetHandlerAsync(HttpContext context, string authenticationScheme)
        {
            return Task.FromResult(Handler);
        }
    }
}
