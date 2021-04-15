// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

#pragma warning disable 1591

namespace Duende.IdentityServer.EntityFramework.Entities
{
    /// <summary>
    /// Models storage for identity providers.
    /// </summary>
    public class OidcIdentityProvider
    {
        /// <summary>
        /// Primary key used for EF
        /// </summary>
        public int Id { get; set; }

        /* general */

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
        public string Type { get; set; } = "oidc";
        
        /* OIDC */

        /// <summary>
        /// The address of the provider
        /// </summary>
        public string Authority { get; set; }
        /// <summary>
        /// The response type
        /// </summary>
        public string ResponseType { get; set; } = "id_token";
        /// <summary>
        /// The client id
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// The client secret
        /// </summary>
        public string ClientSecret { get; set; }
        /// <summary>
        /// The scope
        /// </summary>
        public string Scope { get; set; } = "openid";
    }
}
