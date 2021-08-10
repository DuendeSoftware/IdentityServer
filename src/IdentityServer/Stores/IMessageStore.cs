// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Stores
{
    /// <summary>
    /// Interface for a message store
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public interface IMessageStore<TModel>
    {
        /// <summary>
        /// Writes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>An identifier for the message</returns>
        Task<string> WriteAsync(Message<TModel> message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the message.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Message<TModel>> ReadAsync(string id, CancellationToken cancellationToken = default);
    }
}