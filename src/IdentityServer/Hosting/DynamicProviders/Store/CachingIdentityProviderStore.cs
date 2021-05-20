// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Hosting.DynamicProviders
{
    /// <summary>
    /// Caching decorator for IIdentityProviderStore
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CachingIdentityProviderStore<T> : IIdentityProviderStore
            where T : IIdentityProviderStore
    {
        private readonly IIdentityProviderStore _inner;
        private readonly ICache<IdentityProvider> _cache;
        private readonly IdentityServerOptions _options;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="cache"></param>
        /// <param name="options"></param>
        /// <param name="httpContextAccessor"></param>
        public CachingIdentityProviderStore(T inner, 
            ICache<IdentityProvider> cache,
            IdentityServerOptions options,
            IHttpContextAccessor httpContextAccessor)
        {
            _inner = inner;
            _cache = cache;
            _options = options;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc/>
        public async Task<IdentityProvider> GetBySchemeAsync(string scheme)
        {
            var result = await _cache.GetAsync(scheme);
            if (result == null)
            {
                result = await _inner.GetBySchemeAsync(scheme);
                if (result != null)
                {
                    RemoveCacheEntry(result);
                    await _cache.SetAsync(scheme, result, _options.Caching.IdentityProviderCacheDuration);
                }
            }

            return result;
        }

        // when items are re-added, we remove the corresponding options from the 
        // options monitor since those instances are cached my the authentication handler plumbing
        // this keeps theirs in sync with ours when we re-load from the DB
        void RemoveCacheEntry(IdentityProvider idp)
        {
            var provider = _options.DynamicProviders.FindProviderType(idp.Type);
            if (provider != null)
            {
                var optionsMonitorType = typeof(IOptionsMonitorCache<>).MakeGenericType(provider.OptionsType);
                var optionsCache = _httpContextAccessor.HttpContext.RequestServices.GetService(optionsMonitorType);
                if (optionsCache != null)
                {
                    var mi = optionsMonitorType.GetMethod("TryRemove");
                    if (mi != null)
                    {
                        mi.Invoke(optionsCache, new[] { idp.Scheme });
                    }
                }
            }
        }
    }
}
