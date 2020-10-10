// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Services.KeyManagement
{
    /// <summary>
    /// Interface to model protecting/unprotecting RsaKeyContainer.
    /// </summary>
    public interface ISigningKeyProtector
    {
        /// <summary>
        /// Protects RsaKeyContainer.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        SerializedKey Protect(RsaKeyContainer key);

        /// <summary>
        /// Unprotects RsaKeyContainer.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        RsaKeyContainer Unprotect(SerializedKey key);
    }
}
