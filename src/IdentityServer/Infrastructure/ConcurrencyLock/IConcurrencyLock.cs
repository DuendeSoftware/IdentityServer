// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;

namespace Duende.IdentityServer.Internal
{
    /// <summary>
    /// Interface to model locking.
    /// </summary>
    public interface IConcurrencyLock<T>
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
