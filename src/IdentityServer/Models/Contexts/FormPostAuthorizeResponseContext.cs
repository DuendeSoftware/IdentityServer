// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Collections.Specialized;

namespace Duende.IdentityServer.Models
{
    /// <summary>
    /// Provides the context necessary to render the HTML page from the authorize response for form post.
    /// </summary>
    public class FormPostAuthorizeResponseContext
    {
        /// <summary>
        ///  The URL of the form.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The form data.
        /// </summary>
        public NameValueCollection FormData { get; set; }
    }
}
