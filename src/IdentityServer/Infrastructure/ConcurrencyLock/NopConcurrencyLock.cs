// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;

namespace Duende.IdentityServer.Internal
{
    /// <summary>
    /// Nop implementation.
    /// </summary>
    public class NopConcurrencyLock<T> : IConcurrencyLock<T>
    {
        /// <inheritdoc/>
        public Task LockAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Unlock()
        {
        }
    }
}
