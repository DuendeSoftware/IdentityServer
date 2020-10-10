// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


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

        public Task LockAsync()
        {
            return __newKeyLock.WaitAsync();
        }

        public void Unlock()
        {
            __newKeyLock.Release();
        }
    }
}
