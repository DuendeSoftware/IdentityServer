// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Default custom request validator
    /// </summary>
    internal class DefaultCustomAuthorizeRequestValidator : ICustomAuthorizeRequestValidator
    {
        /// <summary>
        /// Custom validation logic for the authorize request.
        /// </summary>
        /// <param name="context">The context.</param>
        public Task ValidateAsync(CustomAuthorizeRequestValidationContext context)
        {
            return Task.CompletedTask;
        }
    }
}