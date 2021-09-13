// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading;

namespace Duende.IdentityServer.Services
{
    /// <summary>
    /// Implementation of ICancellationTokenService that returns a CancellationToken.None.
    /// </summary>
    public class NoneCancellationTokenService : ICancellationTokenService
    {
        /// <inheritdoc/>
        public CancellationToken CancellationToken => CancellationToken.None;
    }
}
