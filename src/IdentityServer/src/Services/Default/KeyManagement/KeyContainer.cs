// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using Duende.IdentityServer.Models;
using Microsoft.IdentityModel.Tokens;

namespace Duende.IdentityServer.Services.KeyManagement
{
    /// <summary>
    /// Container class for key.
    /// </summary>
    public abstract class KeyContainer
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
        {
            Id = id;
            Created = created;
            KeyType = keyType;
        }

        /// <summary>
        /// Key identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Date key was created.
        /// </summary>
        public DateTime Created { get; set; }

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
