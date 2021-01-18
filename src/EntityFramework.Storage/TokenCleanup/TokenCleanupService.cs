// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.EntityFramework
{
    /// <summary>
    /// Helper to cleanup stale persisted grants and device codes.
    /// </summary>
    public class TokenCleanupService
    {
        private readonly OperationalStoreOptions _options;
        private readonly IPersistedGrantDbContext _persistedGrantDbContext;
        private readonly IOperationalStoreNotification _operationalStoreNotification;
        private readonly ILogger<TokenCleanupService> _logger;

        /// <summary>
        /// Constructor for TokenCleanupService.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="persistedGrantDbContext"></param>
        /// <param name="operationalStoreNotification"></param>
        /// <param name="logger"></param>
        public TokenCleanupService(
            OperationalStoreOptions options,
            IPersistedGrantDbContext persistedGrantDbContext, 
            ILogger<TokenCleanupService> logger,
            IOperationalStoreNotification operationalStoreNotification = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (_options.TokenCleanupBatchSize < 1) throw new ArgumentException("Token cleanup batch size interval must be at least 1");

            _persistedGrantDbContext = persistedGrantDbContext ?? throw new ArgumentNullException(nameof(persistedGrantDbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _operationalStoreNotification = operationalStoreNotification;
        }

        /// <summary>
        /// Method to clear expired persisted grants.
        /// </summary>
        /// <returns></returns>
        public async Task RemoveExpiredGrantsAsync()
        {
            try
            {
                _logger.LogTrace("Querying for expired grants to remove");

                await RemoveGrantsAsync();
                await RemoveDeviceCodesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception removing expired grants: {exception}", ex.Message);
            }
        }

        /// <summary>
        /// Removes the stale persisted grants.
        /// </summary>
        /// <returns></returns>
        protected virtual async Task RemoveGrantsAsync()
        {
            await RemoveExpiredPersistedGrantsAsync();
            if (_options.RemoveConsumedTokens)
            {
                await RemoveConsumedPersistedGrantsAsync();
            }
        }

        /// <summary>
        /// Removes the expired persisted grants.
        /// </summary>
        /// <returns></returns>
        protected virtual async Task RemoveExpiredPersistedGrantsAsync()
        {
            var found = Int32.MaxValue;

            while (found >= _options.TokenCleanupBatchSize)
            {
                var expiredGrants = await _persistedGrantDbContext.PersistedGrants
                    .Where(x => x.Expiration < DateTime.UtcNow)
                    .OrderBy(x => x.Expiration)
                    .Take(_options.TokenCleanupBatchSize)
                    .ToArrayAsync();

                found = expiredGrants.Length;
                _logger.LogInformation("Removing {grantCount} expired grants", found);

                if (found > 0)
                {
                    _persistedGrantDbContext.PersistedGrants.RemoveRange(expiredGrants);
                    await SaveChangesAsync();

                    if (_operationalStoreNotification != null)
                    {
                        await _operationalStoreNotification.PersistedGrantsRemovedAsync(expiredGrants);
                    }
                }
            }
        }

        /// <summary>
        /// Removes the consumed persisted grants.
        /// </summary>
        /// <returns></returns>
        protected virtual async Task RemoveConsumedPersistedGrantsAsync()
        {
            var found = Int32.MaxValue;

            while (found >= _options.TokenCleanupBatchSize)
            {
                var expiredGrants = await _persistedGrantDbContext.PersistedGrants
                    .Where(x => x.ConsumedTime < DateTime.UtcNow)
                    .OrderBy(x => x.ConsumedTime)
                    .Take(_options.TokenCleanupBatchSize)
                    .ToArrayAsync();

                found = expiredGrants.Length;
                _logger.LogInformation("Removing {grantCount} consumed grants", found);

                if (found > 0)
                {
                    _persistedGrantDbContext.PersistedGrants.RemoveRange(expiredGrants);
                    await SaveChangesAsync();

                    if (_operationalStoreNotification != null)
                    {
                        await _operationalStoreNotification.PersistedGrantsRemovedAsync(expiredGrants);
                    }
                }
            }
        }


        /// <summary>
        /// Removes the stale device codes.
        /// </summary>
        /// <returns></returns>
        protected virtual async Task RemoveDeviceCodesAsync()
        {
            var found = Int32.MaxValue;

            while (found >= _options.TokenCleanupBatchSize)
            {
                var expiredCodes = await _persistedGrantDbContext.DeviceFlowCodes
                    .Where(x => x.Expiration < DateTime.UtcNow)
                    .OrderBy(x => x.DeviceCode)
                    .Take(_options.TokenCleanupBatchSize)
                    .ToArrayAsync();

                found = expiredCodes.Length;
                _logger.LogInformation("Removing {deviceCodeCount} device flow codes", found);

                if (found > 0)
                {
                    _persistedGrantDbContext.DeviceFlowCodes.RemoveRange(expiredCodes);
                    await SaveChangesAsync();

                    if (_operationalStoreNotification != null)
                    {
                        await _operationalStoreNotification.DeviceCodesRemovedAsync(expiredCodes);
                    }
                }
            }
        }

        private async Task SaveChangesAsync()
        {
            var count = 3;

            while (count > 0)
            {
                try
                {
                    await _persistedGrantDbContext.SaveChangesAsync();
                    return;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    count--;

                    // we get this if/when someone else already deleted the records
                    // we want to essentially ignore this, and keep working
                    _logger.LogDebug("Concurrency exception removing expired grants: {exception}", ex.Message);

                    foreach (var entry in ex.Entries)
                    {
                        // mark this entry as not attached anymore so we don't try to re-delete
                        entry.State = EntityState.Detached;
                    }
                }
            }

            _logger.LogDebug("Too many concurrency exceptions. Exiting.");
        }
    }
}