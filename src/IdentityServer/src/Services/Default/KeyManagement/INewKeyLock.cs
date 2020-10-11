// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;

namespace Duende.IdentityServer.Services.KeyManagement
{
    /// <summary>
    /// Interface to model locking when a new key is to be created.
    /// </summary>
    public interface INewKeyLock
    {
        /// <summary>
        /// Locks
        /// </summary>
        /// <returns></returns>
        Task LockAsync();
        
        /// <summary>
        /// Unlocks
        /// </summary>
        /// <returns></returns>
        void Unlock();
    }
}
