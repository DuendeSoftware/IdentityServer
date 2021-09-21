// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Services;
using System;

namespace Duende.IdentityServer.Stores
{
    /// <summary>
    /// Caching decorator for IResourceStore
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="IdentityServer.Stores.IResourceStore" />
    public class CachingResourceStore<T> : IResourceStore
        where T : IResourceStore
    {
        private const string AllKey = "__all__";

        private readonly IdentityServerOptions _options;
        
        private readonly ICache<IdentityResource> _identityCache;
        private readonly ICache<ApiScope> _apiScopeCache;
        private readonly ICache<ApiResource> _apiResourceCache;
        private readonly ICache<Resources> _allCache;
        
        private readonly IResourceStore _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingResourceStore{T}"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="inner">The inner.</param>
        /// <param name="identityCache">The identity cache.</param>
        /// <param name="apisCache">The API cache.</param>
        /// <param name="scopeCache"></param>
        /// <param name="allCache">All cache.</param>
        public CachingResourceStore(IdentityServerOptions options, T inner, 
            ICache<IdentityResource> identityCache, 
            ICache<ApiResource> apisCache,
            ICache<ApiScope> scopeCache,
            ICache<Resources> allCache)
        {
            _options = options;
            _inner = inner;
            _identityCache = identityCache;
            _apiResourceCache = apisCache;
            _apiScopeCache = scopeCache;
            _allCache = allCache;
        }

        /// <inheritdoc/>
        public async Task<Resources> GetAllResourcesAsync()
        {
            var key = AllKey;

            var all = await _allCache.GetOrAddAsync(key,
                _options.Caching.ResourceStoreExpiration,
                async () => await _inner.GetAllResourcesAsync());

            return all;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
        {
            return await FindItemsAsync(apiResourceNames, _apiResourceCache, async names => await _inner.FindApiResourcesByNameAsync(names), x => x.Name);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            return await FindItemsAsync(scopeNames, _identityCache, async names => await _inner.FindIdentityResourcesByScopeNameAsync(names), x => x.Name);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            return await FindItemsAsync(scopeNames, _apiResourceCache, async names => await _inner.FindApiResourcesByScopeNameAsync(names), x => x.Name, "ApiResourcesByScopeNames-");
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
        {
            return await FindItemsAsync(scopeNames, _apiScopeCache, async names => await _inner.FindApiScopesByNameAsync(names), x => x.Name);
        }

        async Task<IEnumerable<TItem>> FindItemsAsync<TItem>(IEnumerable<string> names, ICache<TItem> cache, Func<IEnumerable<string>, Task<IEnumerable<TItem>>> getManyFunc, Func<TItem, string> getNameFunc, string keyPrefix = null)
            where TItem : class
        {
            var uncachedNames = new List<string>();
            var cachedItems = new List<TItem>();
            foreach (var name in names)
            {
                var item = await cache.GetAsync(keyPrefix + name);
                if (item != null)
                {
                    cachedItems.Add(item);
                }
                else
                {
                    uncachedNames.Add(name);
                }
            }

            var uncachedItems = await getManyFunc(uncachedNames);
            foreach (var item in uncachedItems)
            {
                await cache.SetAsync(keyPrefix + getNameFunc(item), item, _options.Caching.ResourceStoreExpiration);
            }

            cachedItems.AddRange(uncachedItems);

            return cachedItems;
        }
    }
}
