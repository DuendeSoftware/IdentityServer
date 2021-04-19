// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace Duende.IdentityServer.Services
{
    /// <summary>
    /// IMemoryCache-based implementation of the cache
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="ICache{T}" />
    public class DefaultCache<T> : ICache<T>
        where T : class
    {
        private const string KeySeparator = ":";

        /// <summary>
        /// The memory cache.
        /// </summary>
        protected IMemoryCache Cache { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultCache{T}"/> class.
        /// </summary>
        /// <param name="cache">The cache.</param>
        public DefaultCache(IMemoryCache cache)
        {
            Cache = cache;
        }

        /// <summary>
        /// Used to create the key for the cache based on the data type being cached.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected string GetKey(string key)
        {
            return typeof(T).FullName + KeySeparator + key;
        }

        /// <summary>
        /// Gets the cached data based upon a key index.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        /// The cached item, or <c>null</c> if no item matches the key.
        /// </returns>
        public Task<T> GetAsync(string key)
        {
            key = GetKey(key);
            var item = Cache.Get<T>(key);
            return Task.FromResult(item);
        }

        /// <summary>
        /// Caches the data based upon a key
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="expiration">The expiration.</param>
        /// <returns></returns>
        public Task SetAsync(string key, T item, TimeSpan expiration)
        {
            key = GetKey(key);
            Cache.Set(key, item, expiration);
            return Task.CompletedTask;
        }

        // for testing
        internal void Remove(string key)
        {
            key = GetKey(key);
            Cache.Remove(key);
        }
    }
}