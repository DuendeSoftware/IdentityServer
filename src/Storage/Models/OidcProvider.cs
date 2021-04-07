// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Duende.IdentityServer.Hosting.DynamicProviders
{
    /// <summary>
    /// Models an OIDC identity provider
    /// </summary>
    public class OidcProvider : IdentityProvider
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public OidcProvider()
        {
            Type = "oidc";
        }

        /// <summary>
        /// The base address of the OIDC provider
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

        /// <summary>
        /// Parses the scope into a collection.
        /// </summary>
        public IEnumerable<string> Scopes
        {
            get
            {
                var scopes = Scope?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
                if (!scopes.Contains("openid"))
                {
                    scopes.Add("openid");
                }
                return scopes;
            }
        }
    }
}
