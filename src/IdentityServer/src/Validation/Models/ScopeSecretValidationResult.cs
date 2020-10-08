// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Validation result for API validation
    /// </summary>
    public class ApiSecretValidationResult : ValidationResult
    {
        /// <summary>
        /// Gets or sets the resource.
        /// </summary>
        /// <value>
        /// The resource.
        /// </value>
        public ApiResource Resource { get; set; }
    }
}