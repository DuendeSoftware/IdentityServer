// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Options for the IdentityServer middleware
    /// </summary>
    public class IdentityServerMiddlewareOptions
    {
        /// <summary>
        /// Callback to wire up an authentication middleware
        /// </summary>
        public Action<IApplicationBuilder> AuthenticationMiddleware { get; set; } = (app) => app.UseAuthentication();
    }
}