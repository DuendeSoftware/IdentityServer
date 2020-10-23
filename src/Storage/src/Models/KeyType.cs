// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


namespace Duende.IdentityServer.Models
{
    /// <summary>
    /// Flag to indicate the type of key.
    /// </summary>
    public enum KeyType
    {
        /// <summary>
        /// RSA key.
        /// </summary>
        RSA,
        /// <summary>
        /// RSA key contained in a self-signed X509 certificate.
        /// </summary>
        X509
    }
}
