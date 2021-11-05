// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;
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
        /// Returns the pending login requests for the current user.
        /// </summary>
        Task<IEnumerable<BackchannelUserLoginRequest>> GetPendingLoginRequestsForCurrentUserAsync();
        
        /// <summary>
        /// Returns the login request for the id.
        /// </summary>
        Task<BackchannelUserLoginRequest> GetLoginRequestById(string id);

        /// <summary>
        /// Completes the login request with the provided response for the current user.
        /// </summary>
        Task CompleteRequestByIdAsync(string id, ConsentResponse consent);
    }
}
