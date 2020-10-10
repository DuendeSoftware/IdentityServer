// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


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
