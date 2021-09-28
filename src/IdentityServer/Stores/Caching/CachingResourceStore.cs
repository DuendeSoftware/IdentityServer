// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Services;

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
        
        private readonly ICache<IEnumerable<IdentityResource>> _identityCache;
        private readonly ICache<IEnumerable<ApiResource>> _apiByScopeCache;
        private readonly ICache<IEnumerable<ApiScope>> _apiScopeCache;
        private readonly ICache<IEnumerable<ApiResource>> _apiResourceCache;
        private readonly ICache<Resources> _allCache;
        
        private readonly IResourceStore _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingResourceStore{T}"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="inner">The inner.</param>
        /// <param name="identityCache">The identity cache.</param>
        /// <param name="apiByScopeCache">The API by scope cache.</param>
        /// <param name="apisCache">The API cache.</param>
        /// <param name="scopeCache"></param>
        /// <param name="allCache">All cache.</param>
        public CachingResourceStore(IdentityServerOptions options, T inner, 
            ICache<IEnumerable<IdentityResource>> identityCache, 
            ICache<IEnumerable<ApiResource>> apiByScopeCache,
            ICache<IEnumerable<ApiResource>> apisCache,
            ICache<IEnumerable<ApiScope>> scopeCache,
            ICache<Resources> allCache)
        {
            _options = options;
            _inner = inner;
            _identityCache = identityCache;
            _apiByScopeCache = apiByScopeCache;
            _apiResourceCache = apisCache;
            _apiScopeCache = scopeCache;
            _allCache = allCache;
        }

        private string GetKey(IEnumerable<string> names)
        {
            if (names == null || !names.Any()) return string.Empty;
            return names.OrderBy(x => x).Aggregate((x, y) => x + "," + y);
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
            var key = GetKey(apiResourceNames);

            var apis = await _apiResourceCache.GetOrAddAsync(key,
                _options.Caching.ResourceStoreExpiration,
                async () => await _inner.FindApiResourcesByNameAsync(apiResourceNames));

            return apis;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> names)
        {
            var key = GetKey(names);

            var identities = await _identityCache.GetOrAddAsync(key,
                _options.Caching.ResourceStoreExpiration,
                async () => await _inner.FindIdentityResourcesByScopeNameAsync(names));

            return identities;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> names)
        {
            var key = GetKey(names);

            // this cache key needs a prefix to disambiguate from the other ICache<IEnumerable<ApiResource>> _apiResourceCache cache
            var apis = await _apiByScopeCache.GetOrAddAsync("ApiResourcesByScopeNames:" + key,
                _options.Caching.ResourceStoreExpiration,
                async () => await _inner.FindApiResourcesByScopeNameAsync(names));

            return apis;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
        {
            var key = GetKey(scopeNames);

            var apis = await _apiScopeCache.GetOrAddAsync(key,
                _options.Caching.ResourceStoreExpiration,
                async () => await _inner.FindApiScopesByNameAsync(scopeNames));

            return apis;
        }
    }
}