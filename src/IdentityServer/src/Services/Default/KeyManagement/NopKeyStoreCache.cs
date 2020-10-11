// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Services.KeyManagement
{
    /// <summary>
    /// Nop implementation of ISigningKeyStoreCache that does not cache keys.
    /// </summary>
    class NopKeyStoreCache : ISigningKeyStoreCache
    {
        /// <summary>
        /// Returns null.
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<RsaKeyContainer>> GetKeysAsync()
        {
            return Task.FromResult<IEnumerable<RsaKeyContainer>>(null);
        }

        /// <summary>
        /// Does not cache keys.
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public Task StoreKeysAsync(IEnumerable<RsaKeyContainer> keys, TimeSpan duration)
        {
            return Task.CompletedTask;
        }
    }
}
