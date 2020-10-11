// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Configuration;
using Microsoft.AspNetCore.Authentication;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Services.KeyManagement
{
    /// <summary>
    /// Implementation of IKeyManager that creates, stores, and rotates signing keys.
    /// </summary>
    public class KeyManager : IKeyManager
    {
        private readonly KeyManagementOptions _options;
        private readonly ISigningKeyStore _store;
        private readonly ISigningKeyStoreCache _cache;
        private readonly ISigningKeyProtector _protector;
        private readonly ISystemClock _clock;
        private readonly INewKeyLock _newKeyLock;
        private readonly ILogger<KeyManager> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Constructor for KeyManager
        /// </summary>
        /// <param name="options"></param>
        /// <param name="store"></param>
        /// <param name="cache"></param>
        /// <param name="protector"></param>
        /// <param name="clock"></param>
        /// <param name="newKeyLock"></param>
        /// <param name="logger"></param>
        /// <param name="httpContextAccessor"></param>
        public KeyManager(
            KeyManagementOptions options,
            ISigningKeyStore store,
            ISigningKeyStoreCache cache,
            ISigningKeyProtector protector,
            ISystemClock clock,
            INewKeyLock newKeyLock,
            ILogger<KeyManager> logger,
            IHttpContextAccessor httpContextAccessor = null)
        {
            options.Validate();

            _options = options;
            _store = store;
            _cache = cache;
            _protector = protector;
            _clock = clock;
            _newKeyLock = newKeyLock;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Returns the current signing key.
        /// </summary>
        /// <returns></returns>
        public async Task<RsaKeyContainer> GetCurrentKeyAsync()
        {
            _logger.LogDebug("Getting the current key.");

            var (_, key) = await GetAllKeysInternalAsync();

            if (_logger.IsEnabled(LogLevel.Information))
            {
                var age = _clock.GetAge(key.Created);
                var expiresIn = _options.KeyExpiration.Subtract(age);
                var retiresIn = _options.KeyRetirement.Subtract(age);
                _logger.LogInformation("Active signing key found with kid {kid}. Expires in {KeyExpiration}. Retires in {KeyRetirement}", key.Id, expiresIn, retiresIn);
            }

            return key;
        }

        /// <summary>
        /// Returns all the validation keys.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<RsaKeyContainer>> GetAllKeysAsync()
        {
            _logger.LogDebug("Getting all the keys.");

            var (keys, _) = await GetAllKeysInternalAsync();
            return keys;
        }

        internal async Task<(IEnumerable<RsaKeyContainer>, RsaKeyContainer)> GetAllKeysInternalAsync()
        {
            var cached = true;
            var keys = await GetKeysFromCacheAsync();
            if (!keys.Any())
            {
                cached = false;
                keys = await GetKeysFromStoreAsync();
            }

            // ensure we have at least one active signing key
            var activeKey = GetActiveSigningKey(keys);

            // if we loaded from cache, see if DB has updated key
            if (activeKey == null && cached)
            {
                _logger.LogDebug("Failed to find an active signing key, reloading keys from database.");
            }

            var rotationRequired = false;
            
            // if we don't have an active key, then a new one is about to be created so don't bother running this check
            if (activeKey != null)
            {
                rotationRequired = IsKeyRotationRequired(keys);
                if (rotationRequired && cached)
                {
                    _logger.LogDebug("Key rotation required, reloading keys from database.");
                }
            }

            if (activeKey == null || rotationRequired)
            {
                _logger.LogDebug("Entering new key lock.");

                // need to create new key, but another thread might have already so acquiring lock.
                await _newKeyLock.LockAsync();
                try
                {
                    // check if another thread did the work already
                    keys = await GetKeysFromCacheAsync();

                    if (activeKey == null)
                    {
                        activeKey = GetActiveSigningKey(keys);
                    }
                    if (rotationRequired)
                    {
                        rotationRequired = IsKeyRotationRequired(keys);
                    }

                    if (activeKey == null || rotationRequired)
                    {
                        // still need to do the work, but check if another server did the work already
                        keys = await GetKeysFromStoreAsync();

                        if (activeKey == null)
                        {
                            activeKey = GetActiveSigningKey(keys);
                        }
                        if (rotationRequired)
                        {
                            rotationRequired = IsKeyRotationRequired(keys);
                        }

                        if (activeKey == null || rotationRequired)
                        {
                            if (activeKey == null)
                            {
                                _logger.LogDebug("No active key; new key creation required.");
                            }
                            else
                            {
                                _logger.LogDebug("Approaching key retirement; new key creation required.");
                            }

                            // now we know we need to create the new key
                            (keys, activeKey) = await CreateNewKeyAndAddToCacheAsync();
                        }
                        else
                        {
                            _logger.LogDebug("Another server created new key.");
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Another thread created new key.");
                    }
                }
                finally
                {
                    _logger.LogDebug("Releasing new key lock.");
                    _newKeyLock.Unlock();
                }
            }

            if (activeKey == null)
            {
                _logger.LogError("Failed to create and then load new key.");
                throw new Exception("Failed to create and then load new key.");
            }

            return (keys, activeKey);
        }

        internal async Task<IEnumerable<RsaKeyContainer>> GetKeysFromCacheAsync()
        {
            var cachedKeys = await _cache.GetKeysAsync();
            if (cachedKeys != null)
            {
                _logger.LogDebug("Cache hit when loading all keys.");
                return cachedKeys;
            }

            _logger.LogDebug("Cache miss when loading all keys.");
            return Enumerable.Empty<RsaKeyContainer>();
        }

        internal bool AreAllKeysWithinInitializationDuration(IEnumerable<RsaKeyContainer> keys)
        {
            // the expired check will include retired keys
            keys = FilterExpiredKeys(keys);

            var result = keys
                .All(x =>
                {
                    var age = _clock.GetAge(x.Created);
                    var isNew = _options.IsWithinInitializationDuration(age);
                    return isNew;
                });

            return result;
        }

        internal async Task<IEnumerable<RsaKeyContainer>> FilterAndDeleteRetiredKeysAsync(IEnumerable<RsaKeyContainer> keys)
        {
            var retired = keys
                .Where(x =>
                {
                    var age = _clock.GetAge(x.Created);
                    var isRetired = _options.IsRetired(age);
                    return isRetired;
                })
                .ToArray();

            if (_options.DeleteRetiredKeys && retired.Any())
            {
                await DeleteKeysAsync(retired.Select(x => x.Id));
            }

            var result = keys.Except(retired);
            return result;
        }

        internal async Task DeleteKeysAsync(IEnumerable<string> keys)
        {
            if (keys == null || !keys.Any()) return;

            foreach (var key in keys)
            {
                _logger.LogDebug("Deleting retired key: {kid}.", key);
                await _store.DeleteKeyAsync(key);
            }
        }


        internal IEnumerable<RsaKeyContainer> FilterExpiredKeys(IEnumerable<RsaKeyContainer> keys)
        {
            var result = keys
                .Where(x =>
                {
                    var age = _clock.GetAge(x.Created);
                    var isExpired = _options.IsExpired(age);
                    return !isExpired;
                });

            return result;
        }

        internal async Task CacheKeysAsync(IEnumerable<RsaKeyContainer> keys)
        {
            if (keys?.Any() == true)
            {
                var duration = _options.KeyCacheDuration;

                if (AreAllKeysWithinInitializationDuration(keys))
                {
                    // if all key are new, then we want to use the shorter initialization key cache duration.
                    // this attempts to allow other servers that are slow to write new keys to complete, then we will
                    // have the most up to date keys in the cache sooner.
                    duration = _options.InitializationKeyCacheDuration;
                    if (duration > TimeSpan.Zero)
                    {
                        _logger.LogDebug("Caching keys with InitializationKeyCacheDuration for {InitializationKeyCacheDuration}", _options.InitializationKeyCacheDuration);
                    }
                }
                else if (_options.KeyCacheDuration > TimeSpan.Zero)
                {
                    _logger.LogDebug("Caching keys with KeyCacheDuration for {KeyCacheDuration}", _options.KeyCacheDuration);
                }

                if (duration > TimeSpan.Zero)
                {
                    await _cache.StoreKeysAsync(keys, duration);
                }
            }
        }

        internal async Task<IEnumerable<RsaKeyContainer>> GetKeysFromStoreAsync(bool cache = true)
        {
            var protectedKeys = await _store.LoadKeysAsync();
            if (protectedKeys != null && protectedKeys.Any())
            {
                var keys = protectedKeys.Select(x =>
                    {
                        try
                        {
                            var key = _protector.Unprotect(x);
                            if (key == null)
                            {
                                _logger.LogWarning("Key with kid {kid} failed to unprotect.", x.Id);
                            }
                            return key;
                        }
                        catch(Exception ex)
                        {
                            _logger.LogError(ex, "Error unprotecting key with kid {kid}.", x?.Id);
                        }
                        return null;
                    })
                    .Where(x => x != null)
                    .ToArray().AsEnumerable();

                // retired keys are those that are beyond inclusion, thus we act as if they don't exist.
                keys = await FilterAndDeleteRetiredKeysAsync(keys);
                if (keys.Any())
                {
                    _logger.LogDebug("Keys successfully returned from store.");

                    if (cache)
                    {
                        await CacheKeysAsync(keys);
                    }

                    return keys;
                }
            }

            _logger.LogInformation("No keys returned from store.");

            return Enumerable.Empty<RsaKeyContainer>();
        }

        internal async Task<(IEnumerable<RsaKeyContainer>, RsaKeyContainer)> CreateNewKeyAndAddToCacheAsync()
        {
            var newKey = await CreateAndStoreNewKeyAsync();
            
            var keys = await _cache.GetKeysAsync() ?? Enumerable.Empty<RsaKeyContainer>();
            keys = keys.Append(newKey);

            if (AreAllKeysWithinInitializationDuration(keys))
            {
                // this is meant to allow multiple servers that all start at the same time to have some 
                // time to complete writing their newly created keys to the store. then when all load
                // each other's keys, they should all agree on the oldest key based on created time.
                // it's intended to address the scenario where two servers start, server1 creates a key whose
                // time is earlier than server 2, but server1 is slow to write the key to the store.
                // we don't want server2 to only see server2's key, as it's newer.
                if (_options.InitializationSynchronizationDelay > TimeSpan.Zero)
                {
                    _logger.LogDebug("All keys are new; delaying before reloading keys from store by InitializationSynchronizationDelay for {InitializationSynchronizationDelay}.", _options.InitializationSynchronizationDelay);
                    await Task.Delay(_options.InitializationSynchronizationDelay);
                }
                else
                {
                    _logger.LogDebug("All keys are new; reloading keys from store.");
                }

                // reload in case other new keys were recently created
                keys = await GetKeysFromStoreAsync(false);
            }

            // explicitly cache here since we didn't when we loaded above
            await CacheKeysAsync(keys);

            var activeKey = GetActiveSigningKey(keys);

            return (keys, activeKey);
        }

        internal RsaKeyContainer GetActiveSigningKey(IEnumerable<RsaKeyContainer> keys)
        {
            if (keys == null || !keys.Any()) return null;

            _logger.LogDebug("Looking for an active signing key.");

            var ignoreActivation = false;
            // look for keys past activity delay
            var activeKey = GetActiveSigningKeyInternal(keys, ignoreActivation);
            if (activeKey == null)
            {
                ignoreActivation = true;
                _logger.LogDebug("No active signing key found (respecting the activation delay).");
                
                // none, so check if any of the keys were recently created
                activeKey = GetActiveSigningKeyInternal(keys, ignoreActivation);

                if (activeKey == null)
                {
                    _logger.LogDebug("No active signing key found (ignoring the activation delay).");
                }
            }

            if (activeKey != null && _logger.IsEnabled(LogLevel.Debug))
            {
                var delay = ignoreActivation ? "(ignoring the activation delay)" : "(respecting the activation delay)";
                _logger.LogDebug("Active signing key found " + delay + ".  with kid: {kid}.", activeKey.Id);
            }

            return activeKey;
        }

        internal RsaKeyContainer GetActiveSigningKeyInternal(IEnumerable<RsaKeyContainer> keys, bool ignoreActivationDelay = false)
        {
            if (keys == null) return null;

            keys = keys.Where(key => CanBeUsedForSigning(key, ignoreActivationDelay)).ToArray();
            if (!keys.Any())
            {
                return null;
            }

            // we order by the created date, in essence loading the oldest key
            // this accomodates the scenario where 2 servers create keys at the same time
            // but the first server only reloads the one key it created (and only has the one key for 
            // discovery). we don't want the second server using a key that's not in the first server's
            // discovery document. this will be somewhat mitigated by the initial duration where we 
            // deliberatly ignore the cache.
            var result = keys.OrderBy(x => x.Created).First();
            return result;
        }

        internal bool CanBeUsedForSigning(RsaKeyContainer key, bool ignoreActiveDelay = false)
        {
            if (key == null) return false;

            if (key.KeyType != _options.KeyType)
            {
                _logger.LogTrace("Key {kid} is of type {kty} but server configured for {configuredKty}", key.Id, key.KeyType, _options.KeyType);
                return false;
            }

            var now = _clock.UtcNow;

            // newly created key check
            var start = key.Created;
            if (start > now)
            {
                // if another server created the key in the future (meaning this server's clock is 
                // behind the other), then we will just assume the other server's time for this key. 
                // this is how we can deal with clock skew for recently created keys. 
                now = start;
            }

            if (!ignoreActiveDelay)
            {
                _logger.LogTrace("Checking if key with kid {kid} is active (respecting activation delay).", key.Id);
                start = start.Add(_options.KeyActivationDelay);
            }
            else
            {
                _logger.LogTrace("Checking if key with kid {kid} is active (ignoring activation delay).", key.Id);
            }

            if (start > now)
            {
                _logger.LogTrace("Key with kid {kid} is inactive: the current time is prior to its activation delay.", key.Id);
                return false;
            }

            // expired key check
            var end = key.Created.Add(_options.KeyExpiration);
            if (end < now)
            {
                _logger.LogTrace("Key with kid {kid} is inactive: the current time is past its expiration.", key.Id);
                return false;
            }

            _logger.LogTrace("Key with kid {kid} is active.", key.Id);

            return true;
        }

        internal async Task<RsaKeyContainer> CreateAndStoreNewKeyAsync()
        {
            _logger.LogDebug("Creating new key.");

            var rsa = _options.CreateRsaSecurityKey();
            var now = _clock.UtcNow.DateTime;
            var iss = _httpContextAccessor?.HttpContext?.GetIdentityServerIssuerUri();
            var container = _options.KeyType == KeyType.RSA ?
                new RsaKeyContainer(rsa, now) :
                new X509KeyContainer(rsa, now, _options.KeyRetirement, iss);

            var key = _protector.Protect(container);
            await _store.StoreKeyAsync(key);

            _logger.LogInformation("Created and stored new key with kid {kid}.", container.Id);

            return container;
        }

        internal bool IsKeyRotationRequired(IEnumerable<RsaKeyContainer> keys)
        {
            if (keys == null || !keys.Any()) return true;

            var activeKey = GetActiveSigningKey(keys);
            if (activeKey == null) return true;

            // rotation is needed if: 1) if there are no other keys next in line (meaning younger).
            // and 2) the current activiation key is near expiration (using the delay timeout)

            // get younger keys (which will also filter active key)
            keys = keys.Where(x => x.Created > activeKey.Created).ToArray();

            if (keys.Any())
            {
                // there are younger keys, then they might also be within the window of the key activiation delay
                // so find the youngest one and treat that one as if it's the active key.
                activeKey = keys.OrderByDescending(x => x.Created).First();
            }

            // if no younger keys, then check if we're nearing the expiration of active key
            // and see if that's within the window of activation delay.
            var age = _clock.GetAge(activeKey.Created);
            var diff = _options.KeyExpiration.Subtract(age);
            var needed = (diff <= _options.KeyActivationDelay);

            if (!needed)
            {
                _logger.LogDebug("New key expected to be created in {KeyRotiation}", diff.Subtract(_options.KeyActivationDelay));
            }

            return needed;
        }
    }
}
