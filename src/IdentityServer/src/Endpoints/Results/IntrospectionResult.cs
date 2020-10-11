// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.Extensions;

namespace Duende.IdentityServer.Endpoints.Results
{
    /// <summary>
    /// Result for introspection
    /// </summary>
    /// <seealso cref="IEndpointResult" />
    public class IntrospectionResult : IEndpointResult
    {
        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <value>
        /// The result.
        /// </value>
        public Dictionary<string, object> Entries { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntrospectionResult"/> class.
        /// </summary>
        /// <param name="entries">The result.</param>
        /// <exception cref="System.ArgumentNullException">result</exception>
        public IntrospectionResult(Dictionary<string, object> entries)
        {
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        /// <summary>
        /// Executes the result.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns></returns>
        public Task ExecuteAsync(HttpContext context)
        {
            context.Response.SetNoCache();
            
            return context.Response.WriteJsonAsync(Entries);
        }
    }
}