// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Interface for the token request validator
    /// </summary>
    public interface ITokenRequestValidator
    {
        /// <summary>
        /// Validates the request.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="clientValidationResult">The client validation result.</param>
        /// <returns></returns>
        Task<TokenRequestValidationResult> ValidateRequestAsync(NameValueCollection parameters, ClientSecretValidationResult clientValidationResult);
    }
}