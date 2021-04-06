// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;

namespace Duende.IdentityServer.Hosting.DynamicProviders
{
    // the static cache is assumed to be configured as a singleton in DI
    // when items are re-added, we remove the corresponding options from the 
    // options monitor since those instances are cached my the authentication handler plumbing
    // this keeps theirs in sync with ours when we re-load from the DB
    class IdentityProviderCache
    {
        public class IdentityProviderCacheItem
        {
            public DateTimeOffset Expiration { get; set; }
            public IdentityProvider Item { get; set; }
        }

        private readonly ConcurrentDictionary<string, IdentityProviderCacheItem> _cache =
            new ConcurrentDictionary<string, IdentityProviderCacheItem>();

        private readonly DynamicProviderOptions _options;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISystemClock _clock;

        public IdentityProviderCache(
            ISystemClock clock,
            DynamicProviderOptions options,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _clock = clock;
            _options = options;
            _httpContextAccessor = httpContextAccessor;
        }

        // remove the entry from the IOptionsMonitorCache, since it's an indefinite cache
        void RemoveCacheEntry(IdentityProvider idp)
        {
            var provider = _options.FindProviderType(idp.Type);
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
        
        public void Add(IdentityProvider idp)
        {
            RemoveCacheEntry(idp);
            
            _cache.TryAdd(idp.Scheme, new IdentityProviderCacheItem
            {
                Item = idp,
                Expiration = _clock.UtcNow.Add(_options.ProviderCacheDuration)
            });
        }

        public IdentityProvider Get(string scheme)
        {
            scheme = scheme ?? String.Empty;

            _cache.TryGetValue(scheme, out var item);

            if (item != null && item.Expiration > _clock.UtcNow)
            {
                return item.Item;
            }

            return null;
        }

        public IdentityProvider Remove(string scheme)
        {
            scheme = scheme ?? String.Empty;

            _cache.TryRemove(scheme, out var item);

            return item?.Item;
        }
    }
}
