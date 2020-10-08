// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;

namespace Duende.IdentityServer.Configuration
{
    /// <summary>
    /// Caching options.
    /// </summary>
    public class CachingOptions
    {
        private static readonly TimeSpan Default = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Gets or sets the client store expiration.
        /// </summary>
        /// <value>
        /// The client store expiration.
        /// </value>
        public TimeSpan ClientStoreExpiration { get; set; } = Default;

        /// <summary>
        /// Gets or sets the scope store expiration.
        /// </summary>
        /// <value>
        /// The scope store expiration.
        /// </value>
        public TimeSpan ResourceStoreExpiration { get; set; } = Default;

        /// <summary>
        /// Gets or sets the CORS origin expiration.
        /// </summary>
        public TimeSpan CorsExpiration { get; set; } = Default;
    }
}