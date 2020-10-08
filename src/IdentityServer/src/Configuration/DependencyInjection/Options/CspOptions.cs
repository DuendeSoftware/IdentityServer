// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Configuration
{
    /// <summary>
    /// Options for Content Security Policy
    /// </summary>
    public class CspOptions
    {
        /// <summary>
        /// Gets or sets the minimum CSP level.
        /// </summary>
        public CspLevel Level { get; set; } = CspLevel.Two;

        /// <summary>
        /// Gets or sets a value indicating whether the deprected X-Content-Security-Policy header should be added.
        /// </summary>
        public bool AddDeprecatedHeader { get; set; } = true;
    }
}