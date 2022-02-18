// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;

namespace Duende.SessionManagement;

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
    public Task<QueryResult<UserSession>> QuerySessionsAsync(QueryFilter? filter = null, CancellationToken cancellationToken = default)
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
        HashSet<string>? clientIds = null;
        if (context.ClientIds != null || context.SendBackchannelLogoutNotification)
        {
            clientIds = new HashSet<string>();

            var filter = new QueryFilter
            {
                SubjectId = context.SubjectId,
                SessionId = context.SessionId,
                Count = 100,
            };
            var sessions = await _serverSideTicketStore.QuerySessionsAsync(filter, cancellationToken);
            
            var total = sessions.TotalPages;
            for(var i = 1; i <= total; i++)
            {
                var ids = sessions.Results
                    .Where(x=>x.ClientIds != null)
                    .SelectMany(x => x.ClientIds);

                if (context.ClientIds != null)
                {
                    ids = ids.Where(x => context.ClientIds.Contains(x));
                }

                foreach (var id in ids)
                {
                    clientIds.Add(id);
                }

                if(i < total)
                {
                    filter.Page = i + 1;
                    sessions = await _serverSideTicketStore.QuerySessionsAsync(filter, cancellationToken);
                }
            }
        }

        if (context.RemoveServerSideSessionCookie)
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

            if (context.ClientIds != null)
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
        if (context.SendBackchannelLogoutNotification && clientIds != null)
        {
            await _backChannelLogoutService.SendLogoutNotificationsAsync(new LogoutNotificationContext { 
                SubjectId = context.SubjectId,
                SessionId = context.SessionId,
                ClientIds = clientIds
            });
        }
    }
}
