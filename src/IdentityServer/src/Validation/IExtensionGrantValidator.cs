// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Handles validation of token requests using custom grant types
    /// </summary>
    public interface IExtensionGrantValidator
    {
        /// <summary>
        /// Validates the custom grant request.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        /// A principal
        /// </returns>
        Task ValidateAsync(ExtensionGrantValidationContext context);

        /// <summary>
        /// Returns the grant type this validator can deal with
        /// </summary>
        /// <value>
        /// The type of the grant.
        /// </value>
        string GrantType { get; }
    }
}