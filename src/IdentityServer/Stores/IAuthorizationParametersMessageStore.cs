// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Stores
{
    /// <summary>
    /// Interface for authorization request messages that are sent from the authorization endpoint to the login and consent UI.
    /// </summary>
    public interface IAuthorizationParametersMessageStore
    {
        /// <summary>
        /// Writes the authorization parameters.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The identifier for the stored message.</returns>
        Task<string> WriteAsync(Message<IDictionary<string, string[]>> message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the authorization parameters.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Message<IDictionary<string, string[]>>> ReadAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the authorization parameters.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}