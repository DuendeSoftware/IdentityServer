// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Parser for finding the best secret in an Enumerable List
    /// </summary>
    public interface ISecretsListParser
    {
        /// <summary>
        /// Tries to find the best secret on the context that can be used for authentication
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>
        /// A parsed secret
        /// </returns>
        Task<ParsedSecret> ParseAsync(HttpContext context);

        /// <summary>
        /// Gets all available authentication methods.
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetAvailableAuthenticationMethods();
    }
}
