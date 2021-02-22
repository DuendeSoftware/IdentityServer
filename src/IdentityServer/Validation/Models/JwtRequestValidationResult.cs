// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Security.Claims;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Models the result of JWT request validation.
    /// </summary>
    public class JwtRequestValidationResult : ValidationResult
    {
        /// <summary>
        /// The key/value pairs from the JWT payload of a successfuly validated request.
        /// </summary>
        public IEnumerable<Claim> Payload { get; set; }
    }
}