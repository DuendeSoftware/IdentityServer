// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Duende.IdentityServer.Models
{
    /// <summary>
    /// Serialized key.
    /// </summary>
    public class SerializedKey : KeyMetadata
    {
        /// <summary>
        /// Constructor for SerializedKey.
        /// </summary>
        public SerializedKey()
        {
        }

        /// <summary>
        /// Constructor for SerializedKey.
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="keyType"></param>
        /// <param name="data"></param>
        public SerializedKey(KeyMetadata metadata, KeyType keyType, string data) : base(metadata)
        {
            KeyType = keyType;
            Data = data;
        }

        /// <summary>
        /// The key type.
        /// </summary>
        public KeyType KeyType { get; set; }

        /// <summary>
        /// Serialized data for key.
        /// </summary>
        public string Data { get; set; }
    }
}
