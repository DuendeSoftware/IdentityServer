// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    ///  Device authorization endpoint request validator.
    /// </summary>
    public interface IDeviceAuthorizationRequestValidator
    {
        /// <summary>
        ///  Validates authorize request parameters.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="clientValidationResult"></param>
        /// <returns></returns>
        Task<DeviceAuthorizationRequestValidationResult> ValidateAsync(NameValueCollection parameters, ClientSecretValidationResult clientValidationResult);
    }
}