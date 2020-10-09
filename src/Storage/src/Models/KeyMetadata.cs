// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;

namespace Duende.IdentityServer.Models
{
    /// <summary>
    /// Metadata about a key.
    /// </summary>
    public class KeyMetadata
    {
        /// <summary>
        /// Constructor for KeyMetadata.
        /// </summary>
        protected KeyMetadata() { }

        /// <summary>
        /// Constructor for KeyMetadata.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="created"></param>
        public KeyMetadata(string id, DateTime created)
        {
            Id = id;
            Created = created;
        }

        /// <summary>
        /// Constructor for KeyMetadata.
        /// </summary>
        /// <param name="metadata"></param>
        public KeyMetadata(KeyMetadata metadata)
            : this(metadata.Id, metadata.Created)
        {
        }

        /// <summary>
        /// Key identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Date key was created.
        /// </summary>
        public DateTime Created { get; set; }
    }
}
