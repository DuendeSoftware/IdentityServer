// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Endpoints.Results
{
    /// <summary>
    /// Result for the jwks document
    /// </summary>
    /// <seealso cref="IEndpointResult" />
    public class JsonWebKeysResult : IEndpointResult
    {
        /// <summary>
        /// Gets the web keys.
        /// </summary>
        /// <value>
        /// The web keys.
        /// </value>
        public IEnumerable<JsonWebKey> WebKeys { get; }

        /// <summary>
        /// Gets the maximum age.
        /// </summary>
        /// <value>
        /// The maximum age.
        /// </value>
        public int? MaxAge { get; }

        /// <summary>
        /// Should the result Json be indented
        /// </summary>
        public bool JsonWebKeyResultIndented { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonWebKeysResult" /> class.
        /// </summary>
        /// <param name="webKeys">The web keys.</param>
        /// <param name="maxAge">The maximum age.</param>
        /// <param name="jsonWebKeysResultIndented">Result Indented or not</param>
        public JsonWebKeysResult(IEnumerable<JsonWebKey> webKeys, int? maxAge, bool jsonWebKeysResultIndented)
        {
            WebKeys = webKeys ?? throw new ArgumentNullException(nameof(webKeys));
            MaxAge = maxAge;
            JsonWebKeyResultIndented = jsonWebKeysResultIndented;
        }

        /// <summary>
        /// Executes the result.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns></returns>
        public Task ExecuteAsync(HttpContext context)
        {
            if (MaxAge.HasValue && MaxAge.Value >= 0)
            {
                context.Response.SetCache(MaxAge.Value, "Origin");
            }

            return context.Response.WriteJsonAsync(new { keys = WebKeys }, "application/json; charset=UTF-8", JsonWebKeyResultIndented);
        }
    }
}