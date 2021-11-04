// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Models a validated request to the backchannel authentication endpoint.
    /// </summary>
    public class ValidatedBackchannelAuthenticationRequest : ValidatedRequest
    {
        /// <summary>
        /// Gets or sets a value indicating whether the request was an OpenID Connect request.
        /// </summary>
        public bool IsOpenIdRequest { get; set; }
        
        /// <summary>
        /// Gets or sets the scopes.
        /// </summary>
        public ICollection<string> RequestedScopes { get; set; }

        /// <summary>
        /// Gets or sets the resource indicator.
        /// </summary>
        public ICollection<string> RequestedResourceIndiators { get; set; }
        
        /// <summary>
        /// Gets or sets the authentication context reference classes.
        /// </summary>
        public ICollection<string> AuthenticationContextReferenceClasses { get; set; }

        /// <summary>
        /// Gets or sets the login hint token.
        /// </summary>
        public string LoginHintToken { get; set; }

        /// <summary>
        /// Gets or sets the id token hint.
        /// </summary>
        public string IdTokenHint { get; set; }

        /// <summary>
        /// Gets or sets the login hint.
        /// </summary>
        public string LoginHint { get; set; }

        /// <summary>
        /// Gets or sets the binding message.
        /// </summary>
        public string BindingMessage { get; set; }
        
        /// <summary>
        /// Gets or sets the user code.
        /// </summary>
        public string UserCode { get; set; }

        /// <summary>
        /// Gets or sets the requested expiry if present, otherwise the client configured expiry.
        /// </summary>
        public int Expiry { get; set; }
   }
}