// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Stores
{
    /// <summary>
    /// Interface for refresh token storage
    /// </summary>
    public interface IRefreshTokenStore
    {
        /// <summary>
        /// Stores the refresh token.
        /// </summary>
        /// <param name="refreshToken">The refresh token.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task<string> StoreRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the refresh token.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="refreshToken">The refresh token.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task UpdateRefreshTokenAsync(string handle, RefreshToken refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the refresh token.
        /// </summary>
        /// <param name="refreshTokenHandle">The refresh token handle.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task<RefreshToken> GetRefreshTokenAsync(string refreshTokenHandle, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the refresh token.
        /// </summary>
        /// <param name="refreshTokenHandle">The refresh token handle.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task RemoveRefreshTokenAsync(string refreshTokenHandle, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the refresh tokens.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task RemoveRefreshTokensAsync(string subjectId, string clientId, CancellationToken cancellationToken = default);
    }
}