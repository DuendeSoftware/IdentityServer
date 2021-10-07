// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Duende.IdentityServer.Models
{
    /// <summary>
    /// Models a backchannel authentication request.
    /// </summary>
    public class BackChannelAuthenticationRequest
    {
        /// <summary>
        /// Gets or sets the creation time.
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the life time in seconds.
        /// </summary>
        public int Lifetime { get; set; }

        /// <summary>
        /// Gets or sets the ID of the client.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the subject.
        /// </summary>
        public ClaimsPrincipal Subject { get; set; }

        /// <summary>
        /// Gets or sets the requested scopes.
        /// </summary>
        public IEnumerable<string> RequestedScopes { get; set; }
        
        /// <summary>
        /// Gets or sets the requested resource indicators.
        /// </summary>
        public IEnumerable<string> RequestedResourceIndicators { get; set; }

        /// <summary>
        /// Gets or sets the authentication context reference classes.
        /// </summary>
        public ICollection<string> AuthenticationContextReferenceClasses { get; set; }

        /// <summary>
        /// Gets or sets the binding message.
        /// </summary>
        public string BindingMessage { get; set; }
    }
}