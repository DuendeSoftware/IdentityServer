// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Validator for handling API client authentication.
    /// </summary>
    public interface IApiSecretValidator
    {
        /// <summary>
        /// Tries to authenticate an API client based on the incoming request
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        Task<ApiSecretValidationResult> ValidateAsync(HttpContext context);
    }
}