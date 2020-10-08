// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Services
{
    /// <summary>
    /// The device flow throttling service.
    /// </summary>
    public interface IDeviceFlowThrottlingService
    {
        /// <summary>
        /// Decides if the requesting client and device code needs to slow down.
        /// </summary>
        /// <param name="deviceCode">The device code.</param>
        /// <param name="details">The device code details.</param>
        /// <returns></returns>
        Task<bool> ShouldSlowDown(string deviceCode, DeviceCode details);
    }
}