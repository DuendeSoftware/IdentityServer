// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Interface for sending a user a login request from a backchannel authentication request.
    /// </summary>
    public interface IUserLoginService
    {
        /// <summary>
        /// Sends a notification for the user to login.
        /// </summary>
        Task SendRequestAsync(UserLoginRequest request);
    }

    /// <summary>
    /// 
    /// </summary>
    public class UserLoginRequest
    {
        /// <summary>
        /// Gets or sets the request id.
        /// </summary>
        public string RequestId { get; set; }
        
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