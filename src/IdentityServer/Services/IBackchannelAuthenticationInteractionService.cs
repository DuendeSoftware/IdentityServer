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
        /// Returns the login requests for the subject id.
        /// </summary>
        Task<IEnumerable<BackchannelUserLoginRequest>> GetLoginRequestsForSubjectAsync(string sub);
        
        /// <summary>
        /// Returns the pending login request for the id.
        /// </summary>
        Task<BackchannelUserLoginRequest> GetPendingLoginRequestById(string id);

        /// <summary>
        /// Removes the requests for the id.
        /// </summary>
        Task RemoveLoginRequestAsync(string id);

        /// <summary>
        /// Competes the login request with the provided response.
        /// </summary>
        Task HandleRequestByIdAsync(string id, ConsentResponse consent);
    }
}
