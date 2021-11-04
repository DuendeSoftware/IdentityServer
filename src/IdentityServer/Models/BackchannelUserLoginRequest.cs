// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Security.Claims;

namespace Duende.IdentityServer.Models
{
    /// <summary>
    /// Models the information to initiate a user login request due to a CIBA request.
    /// </summary>
    public class BackchannelUserLoginRequest
    {
        /// <summary>
        /// Gets or sets the id of the request.
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the subject.
        /// </summary>
        public ClaimsPrincipal Subject { get; set; }

        /// <summary>
        /// Gets or sets the binding message.
        /// </summary>
        public string BindingMessage { get; set; }

        /// <summary>
        /// Gets or sets the authentication context reference classes.
        /// </summary>
        public ICollection<string> AuthenticationContextReferenceClasses { get; set; }
    }
}