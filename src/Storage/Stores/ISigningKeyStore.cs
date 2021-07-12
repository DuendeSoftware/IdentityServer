// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Stores
{
    /// <summary>
    /// Interface to model storage of serialized keys.
    /// </summary>
    public interface ISigningKeyStore
    {
        /// <summary>
        /// Returns all the keys in storage.
        /// </summary>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task<IEnumerable<SerializedKey>> LoadKeysAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists new key in storage.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task StoreKeyAsync(SerializedKey key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes key from storage.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task DeleteKeyAsync(string id, CancellationToken cancellationToken = default);
    }
}
