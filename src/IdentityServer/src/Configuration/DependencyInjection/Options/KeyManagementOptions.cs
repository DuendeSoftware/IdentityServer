// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        /// Key size (in bits) of RSA keys. Defaults to 2048.
        /// </summary>
        public int RsaKeySize { get; set; } = 2048;

        /// <summary>
        /// The signing algorightms allowed. 
        /// If none are specified, then "RS256" will be used as the default.
        /// The first in the collection will be used as the default. 
        /// </summary>
        public IEnumerable<string> AllowedSigningAlgorithms { get; set; } = Enumerable.Empty<string>();

        internal string DefaultSigningAlgorithm => AllowedSigningAlgorithms.First();
        
        internal IEnumerable<string> RsaSigningAlgorithms => AllowedSigningAlgorithms.Where(x => x.StartsWith("R") || x.StartsWith("P"));
        internal IEnumerable<string> EcSigningAlgorithms => AllowedSigningAlgorithms.Where(x => x.StartsWith("E"));
        
        internal bool RsaKeyEnabled => RsaSigningAlgorithms.Any();
        internal bool EcKeyEnabled => EcSigningAlgorithms.Any();

        /// <summary>
        /// Wrap keys in X509 certificates. Defaults to false.
        /// </summary>
        public bool WrapKeysInX509Certificate { get; set; }


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

        /// <summary>
        /// Path for storing keys when using the default file system store.
        /// Defaults to the "keys" directory relative to the hosting application.
        /// </summary>
        public string KeyPath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "keys");

        internal void Validate()
        {
            if (AllowedSigningAlgorithms?.Any() != true)
            {
                AllowedSigningAlgorithms = new[] { "RS256" };
            }
            else
            {
                AllowedSigningAlgorithms = AllowedSigningAlgorithms.Distinct().ToArray();
            }
            
            var invalid = AllowedSigningAlgorithms.Where(x => !SupportedSigningAlgorithms.Contains(x)).ToArray();
            if (invalid.Any())
            {
                var values = invalid.Aggregate((x, y) => $"{x}, {y}");
                throw new Exception($"Invalid signing algorithm(s): '{values}'.");
            }

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
