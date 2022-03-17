// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using System;
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
    private readonly IdentityServerOptions _options;
    private readonly IServerSideTicketService _serverSideTicketService;
    private readonly IServerSideSessionStore _serverSideSessionStore;
    private readonly IPersistedGrantStore _persistedGrantStore;
    private readonly IClientStore _clientStore;
    private readonly IBackChannelLogoutService _backChannelLogoutService;

    /// <summary>
    /// Ctor.
    /// </summary>
    public DefaultSessionManagementService(
        IdentityServerOptions options,
        IServerSideTicketService serverSideTicketService, 
        IServerSideSessionStore serverSideSessionStore, 
        IPersistedGrantStore persistedGrantStore, 
        IClientStore clientStore,
        IBackChannelLogoutService backChannelLogoutService)
    {
        _options = options;
        _serverSideTicketService = serverSideTicketService;
        _serverSideSessionStore = serverSideSessionStore;
        _persistedGrantStore = persistedGrantStore;
        _clientStore = clientStore;
        _backChannelLogoutService = backChannelLogoutService;
    }

    /// <inheritdoc/>
    public Task<QueryResult<UserSession>> QuerySessionsAsync(SessionQuery filter = null, CancellationToken cancellationToken = default)
    {
        return _serverSideTicketService.QuerySessionsAsync(filter, cancellationToken);
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
                grantFilter.ClientIds = context.ClientIds;
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
        if (context.SendBackchannelLogoutNotification)
        {
            // we might have more than one, so load them all
            var sessions = await _serverSideTicketService.GetSessionsAsync(
                new SessionFilter
                {
                    SubjectId = context.SubjectId,
                    SessionId = context.SessionId,
                },
                cancellationToken);

            foreach (var session in sessions)
            {
                await _backChannelLogoutService.SendLogoutNotificationsAsync(new LogoutNotificationContext
                {
                    SubjectId = session.SubjectId,
                    SessionId = session.SessionId,
                    Issuer = session.Issuer,
                    ClientIds = session.ClientIds.Where(x => context.ClientIds == null || context.ClientIds.Contains(x))
                });
            }
        }

        if (context.RemoveServerSideSession)
        {
            // delete the cookies
            await _serverSideSessionStore.DeleteSessionsAsync(new SessionFilter
            {
                SubjectId = context.SubjectId,
                SessionId = context.SessionId,
            }, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task RemoveExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var found = Int32.MaxValue;

        while (found >= 0)
        {
            var sessions = await _serverSideTicketService.GetAndRemoveExpiredSessionsAsync(_options.ServerSideSessions.RemoveExpiredSessionsBatchSize, cancellationToken);
            found = sessions.Count;

            foreach (var session in sessions)
            {
                var clients = new List<string>();
                foreach(var clientId in session.ClientIds)
                {
                    var client = await _clientStore.FindEnabledClientByIdAsync(clientId);
                    if (client?.RevokeTokensAtUserLogout == true)
                    {
                        clients.Add(clientId);
                    }
                }

                if (clients.Count > 0)
                {
                    await _persistedGrantStore.RemoveAllAsync(new PersistedGrantFilter { 
                        SubjectId = session.SubjectId,
                        SessionId = session.SessionId,
                        ClientIds = clients
                    });
                }
            }

            if (_options.ServerSideSessions.ExpiredSessionsTriggerBackchannelLogout)
            {
                foreach (var session in sessions)
                {
                    await _backChannelLogoutService.SendLogoutNotificationsAsync(new LogoutNotificationContext
                    {
                        SubjectId = session.SubjectId,
                        SessionId = session.SessionId,
                        Issuer = session.Issuer,
                        ClientIds = session.ClientIds,
                    });
                }
            }
        }
    }
}
