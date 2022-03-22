// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Extensions;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using System;

namespace Duende.IdentityServer.Services;

/// <summary>
/// Extension for IUserSession.
/// </summary>
public static class IUserSessionExtensions
{
    // TODO: remove in 7.0

    /// <summary>
    /// Creates a LogoutNotificationContext for the current user session.
    /// </summary>
    /// <returns></returns>
    [Obsolete("Unused and will be removed in a future version. Use the APIs on the IUserSession directly instead.")]
    public static async Task<LogoutNotificationContext> GetLogoutNotificationContext(this IUserSession session)
    {
        var clientIds = await session.GetClientListAsync();

        if (clientIds.Any())
        {
            var user = await session.GetUserAsync();
            var sub = user.GetSubjectId();
            var sid = await session.GetSessionIdAsync();

            return new LogoutNotificationContext
            {
                SubjectId = sub,
                SessionId = sid,
                ClientIds = clientIds
            };
        }

        return null;
    }
}