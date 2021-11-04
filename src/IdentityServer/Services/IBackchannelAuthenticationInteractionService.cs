// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Services
{
    /// <summary>
    ///  Provide services be used by the user interface to communicate with IdentityServer for backchannel authentication requests.
    /// </summary>
    public interface IBackchannelAuthenticationInteractionService
    {
        /// <summary>
        /// Returns the login requests for the subject id.
        /// </summary>
        Task<IEnumerable<BackChannelAuthenticationRequest>> GetLoginsForSubjectAsync(string sub);
        
        /// <summary>
        /// Returns the login request for the id.
        /// </summary>
        Task<BackChannelAuthenticationRequest> GetLoginById(string id);

        /// <summary>
        /// Removes the requests for the id.
        /// </summary>
        Task RemoveLoginAsync(string id);
    }

    /// <summary>
    /// Default implementation of IBackchannelAuthenticationInteractionService.
    /// </summary>
    public class DefaultBackchannelAuthenticationInteractionService : IBackchannelAuthenticationInteractionService
    {
        private readonly IBackChannelAuthenticationRequestStore _store;

        /// <summary>
        /// Ctor
        /// </summary>
        public DefaultBackchannelAuthenticationInteractionService(IBackChannelAuthenticationRequestStore store)
        {
            _store = store;
        }

        /// <inheritdoc/>
        public Task<BackChannelAuthenticationRequest> GetLoginById(string id)
        {
            return _store.GetByIdAsync(id);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<BackChannelAuthenticationRequest>> GetLoginsForSubjectAsync(string sub)
        {
            return _store.GetAllForUserAsync(sub);
        }

        /// <inheritdoc/>
        public Task RemoveLoginAsync(string id)
        {
            return _store.RemoveByIdAsync(id);
        }
    }
}
