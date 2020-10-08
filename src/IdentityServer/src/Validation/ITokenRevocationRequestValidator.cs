// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Specialized;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Interface for the token revocation request validator
    /// </summary>
    public interface ITokenRevocationRequestValidator
    {
        /// <summary>
        /// Validates the request.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="client">The client.</param>
        /// <returns></returns>
        Task<TokenRevocationRequestValidationResult> ValidateRequestAsync(NameValueCollection parameters, Client client);
    }
}