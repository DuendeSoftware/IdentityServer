// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// A service for parsing secrets found on the request
    /// </summary>
    public interface ISecretParser
    {
        /// <summary>
        /// Tries to find a secret on the context that can be used for authentication
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>
        /// A parsed secret
        /// </returns>
        Task<ParsedSecret> ParseAsync(HttpContext context);

        /// <summary>
        /// Returns the authentication method name that this parser implements
        /// </summary>
        /// <value>
        /// The authentication method.
        /// </value>
        string AuthenticationMethod { get; }
    }
}