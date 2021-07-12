// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Stores
{
    /// <summary>
    /// Interface for reference token storage
    /// </summary>
    public interface IReferenceTokenStore
    {
        /// <summary>
        /// Stores the reference token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task<string> StoreReferenceTokenAsync(Token token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the reference token.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task<Token> GetReferenceTokenAsync(string handle, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the reference token.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task RemoveReferenceTokenAsync(string handle, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the reference tokens.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task RemoveReferenceTokensAsync(string subjectId, string clientId, CancellationToken cancellationToken = default);
    }
}