// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Stores
{
    /// <summary>
    /// Interface for the backchannel authentication request store
    /// </summary>
    public interface IBackChannelAuthenticationRequestStore
    {
        /// <summary>
        /// Creates the request.
        /// </summary>
        Task<string> CreateRequestAsync(BackChannelAuthenticationRequest request);

        /// <summary>
        /// Gets the request.
        /// </summary>
        Task<BackChannelAuthenticationRequest> GetRequestAsync(string requestId);

        /// <summary>
        /// Removes the request.
        /// </summary>
        Task RemoveRequestAsync(string requestId);
   }
}