// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Default custom request validator
    /// </summary>
    internal class DefaultCustomTokenRequestValidator : ICustomTokenRequestValidator
    {
        /// <summary>
        /// Custom validation logic for a token request.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        /// The validation result
        /// </returns>
        public Task ValidateAsync(CustomTokenRequestValidationContext context)
        {
            return Task.CompletedTask;
        }
    }
}