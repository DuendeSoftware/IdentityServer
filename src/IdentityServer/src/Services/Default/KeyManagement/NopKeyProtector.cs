// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;
using System;

namespace Duende.IdentityServer.Services.KeyManagement
{
    /// <summary>
    /// Nop implementation of IKeyProtector that does not protect the keys managed by KeyManager.
    /// </summary>
    public class NopKeyProtector : ISigningKeyProtector
    {
        /// <summary>
        /// Does not protect RsaKeyContainer.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public SerializedKey Protect(RsaKeyContainer key)
        {
            return new SerializedKey
            {
                Id = key.Id,
                KeyType = key.KeyType,
                Created = DateTime.UtcNow,
                Data = KeySerializer.Serialize(key),
            };
        }

        /// <summary>
        /// Does not unprotect RsaKeyContainer.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public RsaKeyContainer Unprotect(SerializedKey key)
        {
            var item = KeySerializer.Deserialize<RsaKeyContainer>(key.Data); 
            if (item.KeyType == KeyType.X509)
            {
                item = KeySerializer.Deserialize<X509KeyContainer>(key.Data);
            }
            return item;
        }
    }
}
