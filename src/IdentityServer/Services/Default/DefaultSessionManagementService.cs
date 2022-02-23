// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Hosting.TicketStore;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Services;

/// <summary>
/// Default session management service
/// </summary>
public class DefaultSessionManagementService : ISessionManagementService
{
    private readonly IServerSideTicketStore _serverSideTicketStore;
    private readonly IServerSideSessionStore _serverSideSessionStore;
    private readonly IPersistedGrantStore _persistedGrantStore;
    private readonly IBackChannelLogoutService _backChannelLogoutService;

    /// <summary>
    /// Ctor.
    /// </summary>
    public DefaultSessionManagementService(IServerSideTicketStore serverSideTicketStore, IServerSideSessionStore serverSideSessionStore, IPersistedGrantStore persistedGrantStore, IBackChannelLogoutService backChannelLogoutService)
    {
        _serverSideTicketStore = serverSideTicketStore;
        _serverSideSessionStore = serverSideSessionStore;
        _persistedGrantStore = persistedGrantStore;
        _backChannelLogoutService = backChannelLogoutService;
    }

    /// <inheritdoc/>
    public Task<QueryResult<UserSession>> QuerySessionsAsync(SessionQuery filter = null, CancellationToken cancellationToken = default)
    {
        return _serverSideTicketStore.QuerySessionsAsync(filter, cancellationToken);
    }

    static readonly string[] OnlyTokenTypes = new[] {
        IdentityServerConstants.PersistedGrantTypes.RefreshToken,
        IdentityServerConstants.PersistedGrantTypes.ReferenceToken,
        IdentityServerConstants.PersistedGrantTypes.AuthorizationCode,
        IdentityServerConstants.PersistedGrantTypes.BackChannelAuthenticationRequest,
    };

    /// <inheritdoc/>
    public async Task RemoveSessionsAsync(RemoveSessionsContext context, CancellationToken cancellationToken = default)
    {
        // build list of clients (if needed)
        List<string> clientIds = null;
        if (context.ClientIds != null || context.SendBackchannelLogoutNotification)
        {
            clientIds = new List<string>();
            
            var sessions = await _serverSideTicketStore.GetSessionsAsync(
                new SessionFilter
                {
                    SubjectId = context.SubjectId,
                    SessionId = context.SessionId,
                }, 
                cancellationToken);

            var ids = sessions.Where(x => x.ClientIds != null)
                    .SelectMany(x => x.ClientIds)
                    .Distinct();

            if (context.ClientIds != null)
            {
                ids = ids.Where(x => context.ClientIds.Contains(x));
            }

            clientIds.AddRange(ids);
        }


        if (context.RemoveServerSideSession)
        {
            // delete the cookies
            await _serverSideSessionStore.DeleteSessionsAsync(new SessionFilter
            {
                SubjectId = context.SubjectId,
                SessionId = context.SessionId,
            });
        }


        if (context.RevokeTokens || context.RevokeConsents)
        {
            // delete the tokens
            var grantFilter = new PersistedGrantFilter
            {
                SubjectId = context.SubjectId,
                SessionId = context.SessionId,
            };

            if (clientIds != null && clientIds.Any())
            {
                grantFilter.ClientIds = clientIds;
            }
            
            if (!context.RevokeTokens || !context.RevokeConsents)
            {
                if (context.RevokeConsents)
                {
                    grantFilter.Type = IdentityServerConstants.PersistedGrantTypes.UserConsent;
                }
                else
                {
                    grantFilter.Types = OnlyTokenTypes;
                }
            }

            await _persistedGrantStore.RemoveAllAsync(grantFilter);
        }


        // send back channel SLO
        if (context.SendBackchannelLogoutNotification && clientIds != null && clientIds.Any())
        {
            await _backChannelLogoutService.SendLogoutNotificationsAsync(new LogoutNotificationContext { 
                SubjectId = context.SubjectId,
                SessionId = context.SessionId,
                ClientIds = clientIds
            });
        }
    }
}
