// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Stores
{
    /// <summary>
    /// Interface for the authorization code store
    /// </summary>
    public interface IAuthorizationCodeStore
    {
        /// <summary>
        /// Stores the authorization code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task<string> StoreAuthorizationCodeAsync(AuthorizationCode code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the authorization code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task<AuthorizationCode> GetAuthorizationCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the authorization code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task RemoveAuthorizationCodeAsync(string code, CancellationToken cancellationToken = default);
   }
}