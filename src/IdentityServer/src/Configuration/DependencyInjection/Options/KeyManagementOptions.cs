// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;
using System;
using static Duende.IdentityServer.IdentityServerConstants;

namespace Duende.IdentityServer.Configuration
{
    /// <summary>
    /// Options to configure behavior of KeyManager.
    /// </summary>
    public class KeyManagementOptions
    {
        /// <summary>
        /// Specifies if key management should be enabled. Defaults to true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The signing algorithm to use.
        /// Defaults to RS256.
        /// </summary>
        public RsaSigningAlgorithm SigningAlgorithm { get; set; } = RsaSigningAlgorithm.RS256;

        /// <summary>
        /// Key size (in bits) of SigningAlgorithm.
        /// </summary>
        public int KeySize => SigningAlgorithm switch
            {
                RsaSigningAlgorithm.RS256 => 256 * 8,
                RsaSigningAlgorithm.RS384 => 384 * 8,
                RsaSigningAlgorithm.RS512 => 512 * 8,

                RsaSigningAlgorithm.PS256 => 256 * 8,
                RsaSigningAlgorithm.PS384 => 384 * 8,
                RsaSigningAlgorithm.PS512 => 512 * 8,
                _ => throw new ArgumentException("Invalid signing algorithm value", nameof(SigningAlgorithm)),
            };

        /// <summary>
        /// Type of key to use. Defaults to RSA.
        /// </summary>
        public KeyType KeyType { get; set; } = KeyType.RSA;

        /// <summary>
        /// When no keys have been created yet, this is the window of time considered to be an initialization 
        /// period to allow all servers to synchronize if the keys are being created for the first time.
        /// Defaults to 5 minutes.
        /// </summary>
        public TimeSpan InitializationDuration { get; set; } = TimeSpan.FromMinutes(5);
        /// <summary>
        /// Delay used when re-loading from the store when the initialization period. It allows
        /// other servers more time to write new keys so other servers can include them.
        /// Defaults to 5 seconds.
        /// </summary>
        public TimeSpan InitializationSynchronizationDelay { get; set; } = TimeSpan.FromSeconds(5);
        /// <summary>
        /// Cache duration when within the initialization period.
        /// Defaults to 1 minute.
        /// </summary>
        public TimeSpan InitializationKeyCacheDuration { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// When in normal operation, duration to cache keys from store.
        /// Defaults to 24 hours.
        /// </summary>
        public TimeSpan KeyCacheDuration { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Time expected to propigate new keys to all servers, and time expected all clients to refresh discovery.
        /// Defaults to 14 days.
        /// </summary>
        public TimeSpan KeyActivationDelay { get; set; } = TimeSpan.FromDays(14);
        
        /// <summary>
        /// Age at which keys will no longer be used for signing, but will still be used in discovery for validation.
        /// Defaults to 90 days.
        /// </summary>
        public TimeSpan KeyExpiration { get; set; } = TimeSpan.FromDays(90);

        /// <summary>
        /// Age at which keys will no longer be used in discovery. they can be deleted at this point.
        /// Defaults to 180 days.
        /// </summary>
        public TimeSpan KeyRetirement { get; set; } = TimeSpan.FromDays(180);

        /// <summary>
        /// Automatically delete retired keys.
        /// Defaults to true.
        /// </summary>
        public bool DeleteRetiredKeys { get; set; } = true;

        /// <summary>
        /// Automatically protect keys in the storage using data protection.
        /// Defaults to true.
        /// </summary>
        public bool DataProtectKeys { get; set; } = true;

        internal void Validate()
        {
            if (InitializationDuration < TimeSpan.Zero) throw new Exception("InitializationDuration must be greater than or equal zero.");
            if (InitializationSynchronizationDelay < TimeSpan.Zero) throw new Exception("InitializationSynchronizationDelay must be greater than or equal zero.");

            if (InitializationKeyCacheDuration < TimeSpan.Zero) throw new Exception("InitializationKeyCacheDuration must be greater than or equal zero.");
            if (KeyCacheDuration < TimeSpan.Zero) throw new Exception("KeyCacheDuration must be greater than or equal zero.");

            if (KeyActivationDelay <= TimeSpan.Zero) throw new Exception("KeyActivationDelay must be greater than zero.");
            if (KeyExpiration <= TimeSpan.Zero) throw new Exception("KeyExpiration must be greater than zero.");
            if (KeyRetirement <= TimeSpan.Zero) throw new Exception("KeyRetirement must be greater than zero.");

            if (KeyExpiration <= KeyActivationDelay) throw new Exception("KeyExpiration must be longer than KeyActivationDelay.");
            if (KeyRetirement <= KeyExpiration) throw new Exception("KeyRetirement must be longer than KeyExpiration.");
        }
    }
}
