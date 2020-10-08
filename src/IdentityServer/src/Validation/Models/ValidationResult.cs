// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Minimal validation result class (base-class for more complext validation results)
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the validation was successful.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the validation is failed; otherwise, <c>false</c>.
        /// </value>
        public bool IsError { get; set; } = true;

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the error description.
        /// </summary>
        /// <value>
        /// The error description.
        /// </value>
        public string ErrorDescription { get; set; }
    }
}