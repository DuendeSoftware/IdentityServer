// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Services.KeyManagement
{
    /// <summary>
    /// Interface to model caching keys loaded from key store.
    /// </summary>
    public interface ISigningKeyStoreCache
    {
        /// <summary>
        /// Returns cached keys.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<RsaKeyContainer>> GetKeysAsync();

        /// <summary>
        /// Caches keys for duration.
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        Task StoreKeysAsync(IEnumerable<RsaKeyContainer> keys, TimeSpan duration);
    }
}
