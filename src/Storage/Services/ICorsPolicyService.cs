// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Services
{
    /// <summary>
    /// Service that determines if CORS is allowed.
    /// </summary>
    public interface ICorsPolicyService
    {
        /// <summary>
        /// Determines whether origin is allowed.
        /// </summary>
        /// <param name="origin">The origin.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task<bool> IsOriginAllowedAsync(string origin, CancellationToken cancellationToken = default);
    }
}
