// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Internal
{
    /// <summary>
    /// Default implementation.
    /// </summary>
    public class DefaultConcurrencyLock<T> : IConcurrencyLock<T>
    {
        static readonly SemaphoreSlim Lock = new SemaphoreSlim(1);

        /// <inheritdoc/>
        public Task LockAsync()
        {
            return Lock.WaitAsync();
        }

        /// <inheritdoc/>
        public void Unlock()
        {
            Lock.Release();
        }
    }
}
