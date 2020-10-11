// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Hosting
{
    /// <summary>
    /// Endpoint result
    /// </summary>
    public interface IEndpointResult
    {
        /// <summary>
        /// Executes the result.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns></returns>
        Task ExecuteAsync(HttpContext context);
    }
}