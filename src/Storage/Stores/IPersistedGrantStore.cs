// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Stores
{
    /// <summary>
    /// Interface for persisting any type of grant.
    /// </summary>
    public interface IPersistedGrantStore
    {
        /// <summary>
        /// Stores the grant.
        /// </summary>
        /// <param name="grant">The grant.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task StoreAsync(PersistedGrant grant, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the grant.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task<PersistedGrant> GetAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all grants based on the filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the grant by key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes all grants based on the filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task RemoveAllAsync(PersistedGrantFilter filter, CancellationToken cancellationToken = default);
    }
}