// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration.DependencyInjection;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Hosting.DynamicProviders
{
    class CachingIdentityProviderStore : IIdentityProviderStore
    {
        private readonly IIdentityProviderStore _inner;
        private readonly IdentityProviderCache _cache;

        public CachingIdentityProviderStore(Decorator<IIdentityProviderStore> inner, 
            IdentityProviderCache schemeCache)
        {
            _inner = inner.Instance;
            _cache = schemeCache;
        }

        public async Task<IdentityProvider> GetBySchemeAsync(string scheme)
        {
            var result = _cache.Get(scheme);
            if (result == null)
            {
                result = await _inner.GetBySchemeAsync(scheme);
                if (result != null)
                {
                    _cache.Add(result);
                }
            }

            return result;
        }
    }
}
