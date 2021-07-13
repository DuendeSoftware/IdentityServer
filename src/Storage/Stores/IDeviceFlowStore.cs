// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Stores
{
    /// <summary>
    /// Interface for the device flow store
    /// </summary>
    public interface IDeviceFlowStore
    {
        /// <summary>
        /// Stores the device authorization request.
        /// </summary>
        /// <param name="deviceCode">The device code.</param>
        /// <param name="userCode">The user code.</param>
        /// <param name="data">The data.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task StoreDeviceAuthorizationAsync(string deviceCode, string userCode, DeviceCode data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds device authorization by user code.
        /// </summary>
        /// <param name="userCode">The user code.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task<DeviceCode> FindByUserCodeAsync(string userCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds device authorization by device code.
        /// </summary>
        /// <param name="deviceCode">The device code.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        Task<DeviceCode> FindByDeviceCodeAsync(string deviceCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates device authorization, searching by user code.
        /// </summary>
        /// <param name="userCode">The user code.</param>
        /// <param name="data">The data.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        Task UpdateByUserCodeAsync(string userCode, DeviceCode data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the device authorization, searching by device code.
        /// </summary>
        /// <param name="deviceCode">The device code.</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        Task RemoveByDeviceCodeAsync(string deviceCode, CancellationToken cancellationToken = default);
    }
}