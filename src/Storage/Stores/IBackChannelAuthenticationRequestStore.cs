// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;
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
        /// Gets the requests.
        /// </summary>
        Task<IEnumerable<BackChannelAuthenticationRequest>> GetAllForUserAsync(string subjectId, string clientId = null);

        /// <summary>
        /// Gets the request.
        /// </summary>
        Task<BackChannelAuthenticationRequest> GetByAuthenticationRequestIdAsync(string requestId);
        
        /// <summary>
        /// Gets the request.
        /// </summary>
        Task<BackChannelAuthenticationRequest> GetByIdAsync(string id);

        /// <summary>
        /// Removes the request.
        /// </summary>
        Task RemoveByAuthenticationRequestIdAsync(string requestId);
        
        /// <summary>
        /// Removes the request.
        /// </summary>
        Task RemoveByIdAsync(string id);
    }
}