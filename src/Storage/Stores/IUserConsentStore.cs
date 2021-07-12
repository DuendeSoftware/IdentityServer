// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Stores
{
    /// <summary>
    /// Interface for user consent storage
    /// </summary>
    public interface IUserConsentStore
    {
        /// <summary>
        /// Stores the user consent.
        /// </summary>
        /// <param name="consent">The consent.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task StoreUserConsentAsync(Consent consent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the user consent.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task<Consent> GetUserConsentAsync(string subjectId, string clientId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the user consent.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task RemoveUserConsentAsync(string subjectId, string clientId, CancellationToken cancellationToken = default);
    }
}