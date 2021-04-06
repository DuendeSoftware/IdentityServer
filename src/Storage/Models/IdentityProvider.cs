// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

namespace Duende.IdentityServer.Models
{
    /// <summary>
    /// Models general storage for an external authentication provider/handler scheme
    /// </summary>
    public class IdentityProvider
    {
        /// <summary>
        /// Scheme name for the provider.
        /// </summary>
        public string Scheme { get; set; }

        /// <summary>
        /// Display name for the provider.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Flag that indicates if the provider should be used.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Protocol type of the provider.
        /// </summary>
        public string Type { get; set; }
    }
}
