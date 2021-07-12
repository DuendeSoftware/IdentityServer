// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Configuration;
using Microsoft.AspNetCore.Authentication;
using Duende.IdentityServer.Extensions;

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
        private readonly IIssuerNameService _issuerNameService;

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
        /// <param name="issuerNameService"></param>
        public KeyManager(
            KeyManagementOptions options,
            ISigningKeyStore store,
            ISigningKeyStoreCache cache,
            ISigningKeyProtector protector,
            ISystemClock clock,
            INewKeyLock newKeyLock,
            ILogger<KeyManager> logger,
            IIssuerNameService issuerNameService)
        {
            options.Validate();

            _options = options;
            _store = store;
            _cache = cache;
            _protector = protector;
            _clock = clock;
            _newKeyLock = newKeyLock;
            _logger = logger;
            _issuerNameService = issuerNameService;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<KeyContainer>> GetCurrentKeysAsync()
        {
            _logger.LogDebug("Getting the current key.");

            var (_, currentKeys) = await GetAllKeysInternalAsync();

            if (_logger.IsEnabled(LogLevel.Information))
            {
                foreach (var key in currentKeys)
                {
                    var age = _clock.GetAge(key.Created);
                    var expiresIn = _options.RotationInterval.Subtract(age);
                    var retiresIn = _options.KeyRetirementAge.Subtract(age);
                    _logger.LogInformation("Active signing key found with kid {kid} for alg {alg}. Expires in {KeyExpiration}. Retires in {KeyRetirement}", key.Id, key.Algorithm, expiresIn, retiresIn);
                }
            }

            return currentKeys;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<KeyContainer>> GetAllKeysAsync()
        {
            _logger.LogDebug("Getting all the keys.");

            var (keys, _) = await GetAllKeysInternalAsync();
            return keys;
        }



        internal async Task<(IEnumerable<KeyContainer> allKeys, IEnumerable<KeyContainer> signingKeys)> GetAllKeysInternalAsync()
        {
            var cached = true;
            var keys = await GetKeysFromCacheAsync();
            if (!keys.Any())
            {
                cached = false;
                keys = await GetKeysFromStoreAsync();
            }

            // ensure we have all of our active signing keys
            IEnumerable<KeyContainer> signingKeys;
            var signingKeysSuccess = TryGetAllCurrentSigningKeys(keys, out signingKeys);

            // if we loaded from cache, see if DB has updated key
            if (!signingKeysSuccess && cached)
            {
                _logger.LogDebug("Not all signing keys current in cache, reloading keys from database.");
            }

            var rotationRequired = false;
            
            // if we don't have an active key, then a new one is about to be created so don't bother running this check
            if (signingKeysSuccess)
            {
                rotationRequired = IsKeyRotationRequired(keys);
                if (rotationRequired && cached)
                {
                    _logger.LogDebug("Key rotation required, reloading keys from database.");
                }
            }

            if (!signingKeysSuccess || rotationRequired)
            {
                _logger.LogDebug("Entering new key lock.");

                // need to create new key, but another thread might have already so acquiring lock.
                await _newKeyLock.LockAsync();
                try
                {
                    // check if another thread did the work already
                    keys = await GetKeysFromCacheAsync();

                    if (!signingKeysSuccess)
                    {
                        signingKeysSuccess = TryGetAllCurrentSigningKeys(keys, out signingKeys);
                    }
                    if (rotationRequired)
                    {
                        rotationRequired = IsKeyRotationRequired(keys);
                    }

                    if (!signingKeysSuccess || rotationRequired)
                    {
                        // still need to do the work, but check if another server did the work already
                        keys = await GetKeysFromStoreAsync();

                        if (!signingKeysSuccess)
                        {
                            signingKeysSuccess = TryGetAllCurrentSigningKeys(keys, out signingKeys); 
                        }
                        if (rotationRequired)
                        {
                            rotationRequired = IsKeyRotationRequired(keys);
                        }

                        if (!signingKeysSuccess || rotationRequired)
                        {
                            if (!signingKeysSuccess)
                            {
                                _logger.LogDebug("No active keys; new key creation required.");
                            }
                            else
                            {
                                _logger.LogDebug("Approaching key retirement; new key creation required.");
                            }

                            // now we know we need to create new keys
                            (keys, signingKeys) = await CreateNewKeysAndAddToCacheAsync();
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

            if (!signingKeys.Any())
            {
                _logger.LogError("Failed to create and then load new keys.");
                throw new Exception("Failed to create and then load new keys.");
            }

            return (keys, signingKeys);
        }

        internal bool IsKeyRotationRequired(IEnumerable<KeyContainer> allKeys)
        {
            if (allKeys == null || !allKeys.Any()) return true;

            var groupedKeys = allKeys.GroupBy(x => x.Algorithm);
            
            var success = groupedKeys.Count() == _options.AllowedSigningAlgorithmNames.Count() &&
                groupedKeys.All(x => _options.AllowedSigningAlgorithmNames.Contains(x.Key));

            if (!success)
            {
                return true;
            }
            
            foreach(var item in groupedKeys)
            {
                var keys = item.AsEnumerable();
                var activeKey = GetCurrentSigningKey(keys);

                if (activeKey == null)
                {
                    return true;
                }

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
                var diff = _options.RotationInterval.Subtract(age);
                var needed = (diff <= _options.PropagationTime);

                if (!needed)
                {
                    _logger.LogDebug("Key rotation not required for alg {alg}; New key expected to be created in {KeyRotiation}", item.Key, diff.Subtract(_options.PropagationTime));
                }
                else
                {
                    _logger.LogDebug("Key rotation required now for alg {alg}.", item.Key);
                    return true;
                }
            }

            return false;
        }

        internal async Task<KeyContainer> CreateAndStoreNewKeyAsync(SigningAlgorithmOptions alg)
        {
            _logger.LogDebug("Creating new key.");

            var now = _clock.UtcNow.UtcDateTime;
            var iss = await _issuerNameService.GetCurrentAsync();

            KeyContainer container = null;

            if (alg.IsRsaKey)
            {
                var rsa = CryptoHelper.CreateRsaSecurityKey(_options.RsaKeySize);
                
                container = alg.UseX509Certificate ?
                    new X509KeyContainer(rsa, alg.Name, now, _options.KeyRetirementAge, iss) :
                    (KeyContainer)new RsaKeyContainer(rsa, alg.Name, now);
            }
            else if (alg.IsEcKey)
            {
                var ec = CryptoHelper.CreateECDsaSecurityKey(CryptoHelper.GetCurveNameFromSigningAlgorithm(alg.Name));
                // X509 certs don't currently work with EC keys.
                container = //_options.WrapKeysInX509Certificate ? //new X509KeyContainer(ec, alg, now, _options.KeyRetirementAge, iss) :
                    (KeyContainer) new EcKeyContainer(ec, alg.Name, now);
            }
            else
            {
                throw new Exception($"Invalid alg '{alg}'");
            }

            var key = _protector.Protect(container);
            await _store.StoreKeyAsync(key);

            _logger.LogInformation("Created and stored new key with kid {kid}.", container.Id);

            return container;
        }
        
        internal async Task<IEnumerable<KeyContainer>> GetKeysFromCacheAsync()
        {
            var cachedKeys = await _cache.GetKeysAsync();
            if (cachedKeys != null)
            {
                _logger.LogDebug("Cache hit when loading all keys.");
                return cachedKeys;
            }

            _logger.LogDebug("Cache miss when loading all keys.");
            return Enumerable.Empty<KeyContainer>();
        }

        internal bool AreAllKeysWithinInitializationDuration(IEnumerable<KeyContainer> keys)
        {
            if (_options.InitializationDuration == TimeSpan.Zero)
            {
                return false;
            }

            // the expired check will also filter retired keys
            keys = FilterExpiredKeys(keys);

            var result = keys.All(x =>
                {
                    var age = _clock.GetAge(x.Created);
                    var isNew = _options.IsWithinInitializationDuration(age);
                    return isNew;
                });

            return result;
        }

        internal async Task<IEnumerable<KeyContainer>> FilterAndDeleteRetiredKeysAsync(IEnumerable<KeyContainer> keys)
        {
            var retired = keys
                .Where(x =>
                {
                    var age = _clock.GetAge(x.Created);
                    var isRetired = _options.IsRetired(age);
                    return isRetired;
                })
                .ToArray();

            if (retired.Any())
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    var ids = retired.Select(x => x.Id).ToArray();
                    _logger.LogTrace("Filtered retired keys from store: {kids}", ids.Aggregate((x, y) => $"{x},{y}"));
                }

                if (_options.DeleteRetiredKeys)
                {
                    var ids = retired.Select(x => x.Id).ToArray();
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Deleting retired keys from store: {kids}", ids.Aggregate((x, y) => $"{x},{y}"));
                    }
                    await DeleteKeysAsync(ids);
                }
            }

            var result = keys.Except(retired).ToArray();
            return result;
        }

        internal async Task DeleteKeysAsync(IEnumerable<string> keys)
        {
            if (keys == null || !keys.Any()) return;

            foreach (var key in keys)
            {
                await _store.DeleteKeyAsync(key);
            }
        }

        internal IEnumerable<KeyContainer> FilterExpiredKeys(IEnumerable<KeyContainer> keys)
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

        internal async Task CacheKeysAsync(IEnumerable<KeyContainer> keys)
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

        internal async Task<IEnumerable<KeyContainer>> GetKeysFromStoreAsync(bool cache = true)
        {
            _logger.LogDebug("Loading keys from store.");
            
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
                          catch (Exception ex)
                          {
                              _logger.LogError(ex, "Error unprotecting key with kid {kid}.", x?.Id);
                          }
                          return null;
                      })
                    .Where(x => x != null)
                    .ToArray()
                    .AsEnumerable();

                if (_logger.IsEnabled(LogLevel.Trace) && keys.Any())
                {
                    var ids = keys.Select(x => x.Id).ToArray();
                    _logger.LogTrace("Loaded keys from store: {kids}", ids.Aggregate((x, y) => $"{x},{y}"));
                }

                // retired keys are those that are beyond inclusion, thus we act as if they don't exist.
                keys = await FilterAndDeleteRetiredKeysAsync(keys);
                
                if (_logger.IsEnabled(LogLevel.Trace) && keys.Any())
                {
                    var ids = keys.Select(x => x.Id).ToArray();
                    _logger.LogTrace("Remaining keys after filter: {kids}", ids.Aggregate((x, y) => $"{x},{y}"));
                }

                // only use keys that are allowed
                keys = keys.Where(x => _options.AllowedSigningAlgorithmNames.Contains(x.Algorithm)).ToArray();
                if (_logger.IsEnabled(LogLevel.Trace) && keys.Any())
                {
                    var ids = keys.Select(x => x.Id).ToArray();
                    _logger.LogTrace("Keys with allowed alg from store: {kids}", ids.Aggregate((x, y) => $"{x},{y}"));
                }

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

            return Enumerable.Empty<KeyContainer>();
        }




        internal async Task<(IEnumerable<KeyContainer> allKeys, IEnumerable<KeyContainer> activeKeys)> CreateNewKeysAndAddToCacheAsync()
        {
            var keys = new List<KeyContainer>();
            keys.AddRange(await _cache.GetKeysAsync() ?? Enumerable.Empty<KeyContainer>());

            foreach (var alg in _options.SigningAlgorithms)
            {
                var newKey = await CreateAndStoreNewKeyAsync(alg);
                keys.Add(newKey);
            }
            
            if (AreAllKeysWithinInitializationDuration(keys))
            {
                // this is meant to allow multiple servers that all start at the same time to have some 
                // time to complete writing their newly created keys to the store. then when all load
                // each other's keys, they should all agree on the oldest key based on created time.
                // it's intended to address the scenario where two servers start, server1 creates a key whose
                // time is earlier than server2, but server1 is slow to write the key to the store.
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
                keys = new List<KeyContainer>(await GetKeysFromStoreAsync(false));
            }

            // explicitly cache here since we didn't when we loaded above
            await CacheKeysAsync(keys);

            var activeKeys = GetCurrentSigningKeys(keys);

            return (keys, activeKeys);
        }

        internal bool TryGetAllCurrentSigningKeys(IEnumerable<KeyContainer> keys, out IEnumerable<KeyContainer> signingKeys)
        {
            signingKeys = GetCurrentSigningKeys(keys);
            
            var success = signingKeys.Count() == _options.AllowedSigningAlgorithmNames.Count() &&
                signingKeys.All(x => _options.AllowedSigningAlgorithmNames.Contains(x.Algorithm));
            
            return success;
        }

        internal IEnumerable<KeyContainer> GetCurrentSigningKeys(IEnumerable<KeyContainer> keys)
        {
            if (keys == null || !keys.Any())
            {
                return Enumerable.Empty<KeyContainer>();
            }

            _logger.LogDebug("Looking for active signing keys.");

            var list = new List<KeyContainer>();
            var groupedKeys = keys.GroupBy(x => x.Algorithm);
            foreach (var item in groupedKeys)
            {
                _logger.LogDebug("Looking for an active signing key for alg {alg}.", item.Key);
                
                var activeKey = GetCurrentSigningKey(item);
                if (activeKey != null)
                {
                    _logger.LogDebug("Found active signing key for alg {alg} with kid {kid}.", item.Key, activeKey.Id);
                    list.Add(activeKey);
                }
                else
                {
                    _logger.LogDebug("Failed to find active signing key for alg {alg}.", item.Key);
                }
            }

            return list;
        }

        internal KeyContainer GetCurrentSigningKey(IEnumerable<KeyContainer> keys)
        {
            if (keys == null || !keys.Any()) return null;

            var ignoreActivation = false;
            // look for keys past activity delay
            var activeKey = GetCurrentSigningKeyInternal(keys, ignoreActivation);
            if (activeKey == null)
            {
                ignoreActivation = true;
                _logger.LogDebug("No active signing key found (respecting the activation delay).");

                // none, so check if any of the keys were recently created
                activeKey = GetCurrentSigningKeyInternal(keys, ignoreActivation);

                if (activeKey == null)
                {
                    _logger.LogDebug("No active signing key found (ignoring the activation delay).");
                }
            }

            if (activeKey != null && _logger.IsEnabled(LogLevel.Debug))
            {
                var delay = ignoreActivation ? "(ignoring the activation delay)" : "(respecting the activation delay)";
                _logger.LogDebug("Active signing key found " + delay + " with kid: {kid}.", activeKey.Id);
            }

            return activeKey;
        }

        internal KeyContainer GetCurrentSigningKeyInternal(IEnumerable<KeyContainer> keys, bool ignoreActivationDelay = false)
        {
            if (keys == null) return null;

            keys = keys.Where(key => CanBeUsedAsCurrentSigningKey(key, ignoreActivationDelay)).ToArray();
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

        internal bool CanBeUsedAsCurrentSigningKey(KeyContainer key, bool ignoreActiveDelay = false)
        {
            if (key == null) return false;

            var alg = _options.SigningAlgorithms.SingleOrDefault(x => x.Name == key.Algorithm);
            if (alg == null)
            {
                _logger.LogTrace("Key {kid} signing algorithm {alg} not allowed by server options.", key.Id, key.Algorithm);
                return false;
            }

            if (alg.UseX509Certificate && !key.HasX509Certificate)
            {
                _logger.LogTrace("Server configured to wrap keys in X509 certs, but key {kid} is not wrapped in cert.", key.Id);
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
                start = start.Add(_options.PropagationTime);
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
            var end = key.Created.Add(_options.RotationInterval);
            if (end < now)
            {
                _logger.LogTrace("Key with kid {kid} is inactive: the current time is past its expiration.", key.Id);
                return false;
            }

            _logger.LogTrace("Key with kid {kid} is active.", key.Id);

            return true;
        }
    }
}
