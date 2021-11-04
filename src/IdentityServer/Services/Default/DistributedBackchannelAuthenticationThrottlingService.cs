// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Distributed;

namespace Duende.IdentityServer.Services
{
    /// <summary>
    /// Implementation of IBackchannelAuthenticationThrottlingService that uses the IDistributedCache.
    /// </summary>
    public class DistributedBackchannelAuthenticationThrottlingService : IBackchannelAuthenticationThrottlingService
    {
        private readonly IDistributedCache _cache;
        private readonly ISystemClock _clock;
        private readonly IdentityServerOptions _options;

        private const string KeyPrefix = "backchannel_";

        /// <summary>
        /// Ctor
        /// </summary>
        public DistributedBackchannelAuthenticationThrottlingService(
            IDistributedCache cache,
            ISystemClock clock,
            IdentityServerOptions options)
        {
            _cache = cache;
            _clock = clock;
            _options = options;
        }
        
        /// <inheritdoc/>
        public async Task<bool> ShouldSlowDown(string requestId, BackChannelAuthenticationRequest details)
        {
            if (requestId == null) throw new ArgumentNullException(nameof(requestId));

            var key = KeyPrefix + requestId;
            var options = new DistributedCacheEntryOptions { AbsoluteExpiration = _clock.UtcNow.AddSeconds(details.Lifetime) };

            var lastSeenAsString = await _cache.GetStringAsync(key);

            // record new
            if (lastSeenAsString == null)
            {
                await _cache.SetStringAsync(key, _clock.UtcNow.ToString("O"), options);
                return false;
            }

            // check interval
            if (DateTime.TryParse(lastSeenAsString, out var lastSeen))
            {
                if (_clock.UtcNow < lastSeen.AddSeconds(_options.DeviceFlow.Interval))
                {
                    await _cache.SetStringAsync(key, _clock.UtcNow.ToString("O"), options);
                    return true;
                }
            }

            // store current and continue
            await _cache.SetStringAsync(key, _clock.UtcNow.ToString("O"), options);

            return false;
        }
    }
}