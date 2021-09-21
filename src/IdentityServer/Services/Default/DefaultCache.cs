// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System;
using Microsoft.Extensions.Logging;

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
        private const string KeySeparator = "-";

        /// <summary>
        /// The memory cache.
        /// </summary>
        protected IMemoryCache Cache { get; }

        /// <summary>
        /// The logger.
        /// </summary>
        protected ILogger<DefaultCache<T>> Logger { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultCache{T}"/> class.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="logger">The logger.</param>
        public DefaultCache(IMemoryCache cache, ILogger<DefaultCache<T>> logger)
        {
            Cache = cache;
            Logger = logger;
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

        /// <inheritdoc/>
        public Task<T> GetAsync(string key)
        {
            key = GetKey(key);
            var item = Cache.Get<T>(key);
            return Task.FromResult(item);
        }

        /// <inheritdoc/>
        public Task SetAsync(string key, T item, TimeSpan expiration)
        {
            key = GetKey(key);
            Cache.Set(key, item, expiration);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveAsync(string key)
        {
            key = GetKey(key);
            Cache.Remove(key);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<T> GetOrAddAsync(string key, TimeSpan duration, Func<Task<T>> get)
        {
            if (get == null) throw new ArgumentNullException(nameof(get));
            if (key == null) return null;

            var item = await GetAsync(key);

            if (item == null)
            {
                Logger.LogTrace("Cache miss for {cacheKey}", key);

                item = await get();

                if (item != null)
                {
                    Logger.LogTrace("Setting item in cache for {cacheKey}", key);
                    await SetAsync(key, item, duration);
                }
            }
            else
            {
                Logger.LogTrace("Cache hit for {cacheKey}", key);
            }

            return item;
        }
    }
}