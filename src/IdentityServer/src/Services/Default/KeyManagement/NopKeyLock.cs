// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;

namespace Duende.IdentityServer.Services.KeyManagement
{
    /// <summary>
    /// Nop implementation.
    /// </summary>
    public class NopKeyLock : INewKeyLock
    {
        public Task LockAsync()
        {
            return Task.CompletedTask;
        }

        public void Unlock()
        {
        }
    }
}
