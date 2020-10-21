// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Services.KeyManagement
{
    /// <summary>
    /// Interface to model loading the current singing key, as well as all keys used in OIDC discovery.
    /// </summary>
    public interface IKeyManager
    {
        /// <summary>
        /// Returns the current signing key.
        /// </summary>
        /// <returns></returns>
        Task<RsaKeyContainer> GetCurrentKeyAsync();

        /// <summary>
        /// Returns all the validation keys.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<RsaKeyContainer>> GetAllKeysAsync();
    }
}
