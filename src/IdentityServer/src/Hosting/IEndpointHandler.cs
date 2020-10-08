// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Hosting
{
    /// <summary>
    /// Endpoint handler
    /// </summary>
    public interface IEndpointHandler
    {
        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns></returns>
        Task<IEndpointResult> ProcessAsync(HttpContext context);
    }
}