// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Collections.Generic;

namespace Duende.IdentityServer.Models
{
    /// <summary>
    /// Provides the context necessary to construct a logout notificaiton.
    /// </summary>
    public class LogoutNotificationContext
    {
        /// <summary>
        ///  The SubjectId of the user.
        /// </summary>
        public string SubjectId { get; set; }

        /// <summary>
        /// The session Id of the user's authentication session.
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// The list of client Ids that the user has authenticated to.
        /// </summary>
        public IEnumerable<string> ClientIds { get; set; }
    }
}
