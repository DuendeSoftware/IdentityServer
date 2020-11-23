// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Extensions;

namespace Duende.IdentityServer.ResponseHandling
{
    /// <summary>
    /// Indicates interaction outcome for user on authorization endpoint.
    /// </summary>
    public class InteractionResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether the user must login.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is login; otherwise, <c>false</c>.
        /// </value>
        public bool IsLogin { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user must consent.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is consent; otherwise, <c>false</c>.
        /// </value>
        public bool IsConsent { get; set; }

        /// <summary>
        /// Gets a value indicating whether the result is an error.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is error; otherwise, <c>false</c>.
        /// </value>
        public bool IsError => Error != null;

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the error description.
        /// </summary>
        /// <value>
        /// The error description.
        /// </value>
        public string ErrorDescription { get; set; }

        /// <summary>
        /// Gets a value indicating whether the user must be redirected to a custom page.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is redirect; otherwise, <c>false</c>.
        /// </value>
        public bool IsRedirect => RedirectUrl.IsPresent();

        /// <summary>
        /// Gets or sets the URL for the custom page.
        /// </summary>
        /// <value>
        /// The redirect URL.
        /// </value>
        public string RedirectUrl { get; set; }
    }
}