// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Interface for the introspection request validator
    /// </summary>
    public interface IIntrospectionRequestValidator
    {
        /// <summary>
        /// Validates the request.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="api">The API.</param>
        /// <returns></returns>
        Task<IntrospectionRequestValidationResult> ValidateAsync(NameValueCollection parameters, ApiResource api);
    }
}