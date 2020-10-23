// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;

namespace Duende.IdentityServer.Services.KeyManagement
{
    /// <summary>
    /// Nop implementation.
    /// </summary>
    public class NopKeyLock : INewKeyLock
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
