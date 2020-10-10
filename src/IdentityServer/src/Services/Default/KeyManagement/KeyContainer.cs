// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using Duende.IdentityServer.Models;
using Microsoft.IdentityModel.Tokens;

namespace Duende.IdentityServer.Services.KeyManagement
{
    /// <summary>
    /// Container class for key.
    /// </summary>
    public abstract class KeyContainer : KeyMetadata
    {
        /// <summary>
        /// Constructor for KeyContainer.
        /// </summary>
        public KeyContainer()
        {
        }

        /// <summary>
        /// Constructor for RsaKeyContainer.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="created"></param>
        /// <param name="keyType"></param>
        public KeyContainer(string id, DateTime created, KeyType keyType)
            : base(id, created)
        {
            KeyType = keyType;
        }

        /// <summary>
        /// Key type.
        /// </summary>
        public KeyType KeyType { get; set; }

        /// <summary>
        /// Creates AsymmetricSecurityKey.
        /// </summary>
        /// <returns></returns>
        public abstract AsymmetricSecurityKey ToSecurityKey();
    }
}
