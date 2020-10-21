// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Services.KeyManagement
{
    /// <summary>
    /// Default implementation.
    /// </summary>
    public class DefaultKeyLock : INewKeyLock
    {
        static SemaphoreSlim __newKeyLock = new SemaphoreSlim(1);

        /// <inheritdoc/>
        public Task LockAsync()
        {
            return __newKeyLock.WaitAsync();
        }

        /// <inheritdoc/>
        public void Unlock()
        {
            __newKeyLock.Release();
        }
    }
}
