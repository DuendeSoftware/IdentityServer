// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Duende.IdentityServer.Models
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
        /// Ctor
        /// </summary>
        public OidcProvider(IdentityProvider other) : base(other)
        {
            if (other.Type != "oidc") throw new Exception("Invalid 'type'");
        }

        /// <summary>
        /// The base address of the OIDC provider.
        /// </summary>
        public string Authority
        {
            get => this["Authority"];
            set => this["Authority"] = value;
        }
        /// <summary>
        /// The response type. Defaults to "id_token".
        /// </summary>
        public string ResponseType 
        {
            get => this["ResponseType"] ?? "id_token";
            set => this["ResponseType"] = value;
        }
        /// <summary>
        /// The client id.
        /// </summary>
        public string ClientId 
        {
            get => this["ClientId"];
            set => this["ClientId"] = value;
        }
        /// <summary>
        /// The client secret. By default this is the plaintext client secret and great consideration should be taken if this value is to be stored as plaintext in the store.
        /// </summary>
        public string ClientSecret 
        {
            get => this["ClientSecret"];
            set => this["ClientSecret"] = value;
        }
        /// <summary>
        /// Space separated list of scope values.
        /// </summary>
        public string Scope
        {
            get => this["Scope"] ?? "openid";
            set => this["Scope"] = value;
        }

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
