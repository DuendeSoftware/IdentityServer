// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.IdentityModel.Tokens;

namespace Duende.IdentityServer.Models
{
    /// <summary>
    /// Information about a security key
    /// </summary>
    public class SecurityKeyInfo
    {
        /// <summary>
        /// The key
        /// </summary>
        public SecurityKey Key { get; set; }

        /// <summary>
        /// The signing algorithm
        /// </summary>
        public string SigningAlgorithm { get; set; }
    }
}