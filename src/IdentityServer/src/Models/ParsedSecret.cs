// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;

namespace Duende.IdentityServer.Models
{
    /// <summary>
    /// Represents a secret extracted from the HttpContext
    /// </summary>
    public class ParsedSecret
    {
        /// <summary>
        /// Gets or sets the identifier associated with this secret
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the credential to verify the secret
        /// </summary>
        /// <value>
        /// The credential.
        /// </value>
        public object Credential { get; set; }

        /// <summary>
        /// Gets or sets the type of the secret
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets additional properties.
        /// </summary>
        /// <value>
        /// The properties.
        /// </value>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }
}