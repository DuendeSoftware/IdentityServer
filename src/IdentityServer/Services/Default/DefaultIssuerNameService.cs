// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Http;

namespace Duende.IdentityServer.Services
{
    /// <summary>
    /// Abstracts issuer name access
    /// </summary>
    public class DefaultIssuerNameService : IIssuerNameService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor</param>
        public DefaultIssuerNameService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        
        /// <inheritdoc />
        public Task<string> GetCurrentAsync()
        {
            return Task.FromResult(_httpContextAccessor.HttpContext.GetIdentityServerIssuerUri());
        }
    }
}