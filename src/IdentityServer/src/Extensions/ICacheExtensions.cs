// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Duende.IdentityServer.Services;

namespace Duende.IdentityServer.Extensions
{
    /// <summary>
    /// Extensions for ICache
    /// </summary>
    public static class ICacheExtensions
    {
        /// <summary>
        /// Attempts to get an item from the cache. If the item is not found, the <c>get</c> function is used to
        /// obtain the item and populate the cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cache">The cache.</param>
        /// <param name="key">The key.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="get">The get function.</param>
        /// <param name="logger">The logger.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">cache
        /// or
        /// get</exception>
        /// <exception cref="ArgumentNullException">cache
        /// or
        /// get</exception>
        public static async Task<T> GetAsync<T>(this ICache<T> cache, string key, TimeSpan duration, Func<Task<T>> get, ILogger logger)
            where T : class
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            if (get == null) throw new ArgumentNullException(nameof(get));
            if (key == null) return null;

            var item = await cache.GetAsync(key);

            if (item == null)
            {
                logger.LogTrace("Cache miss for {cacheKey}", key);

                item = await get();

                if (item != null)
                {
                    logger.LogTrace("Setting item in cache for {cacheKey}", key);
                    await cache.SetAsync(key, item, duration);
                }
            }
            else
            {
                logger.LogTrace("Cache hit for {cacheKey}", key);
            }

            return item;
        }
    }
}
