// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Security.Claims;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Validation result for userinfo requests
    /// </summary>
    public class UserInfoRequestValidationResult : ValidationResult
    {
        /// <summary>
        /// Gets or sets the token validation result.
        /// </summary>
        /// <value>
        /// The token validation result.
        /// </value>
        public TokenValidationResult TokenValidationResult { get; set; }

        /// <summary>
        /// Gets or sets the subject.
        /// </summary>
        /// <value>
        /// The subject.
        /// </value>
        public ClaimsPrincipal Subject { get; set; }
    }
}