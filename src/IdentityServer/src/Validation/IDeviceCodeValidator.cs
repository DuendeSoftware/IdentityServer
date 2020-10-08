// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// The device code validator
    /// </summary>
    public interface IDeviceCodeValidator
    {
        /// <summary>
        /// Validates the device code.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        Task ValidateAsync(DeviceCodeValidationContext context);
    }
}