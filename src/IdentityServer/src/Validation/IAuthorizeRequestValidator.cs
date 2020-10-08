// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Specialized;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    ///  Authorize endpoint request validator.
    /// </summary>
    public interface IAuthorizeRequestValidator
    {
        /// <summary>
        ///  Validates authorize request parameters.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="subject"></param>
        /// <returns></returns>
        Task<AuthorizeRequestValidationResult> ValidateAsync(NameValueCollection parameters, ClaimsPrincipal subject = null);
    }
}