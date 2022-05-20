// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using System;
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
    public DefaultSessionManagementService(
        IServerSideTicketStore serverSideTicketStore,
        IServerSideSessionStore serverSideSessionStore,
        IPersistedGrantStore persistedGrantStore,
        IBackChannelLogoutService backChannelLogoutService)
    {
        _serverSideTicketStore = serverSideTicketStore;
        _serverSideSessionStore = serverSideSessionStore;
        _persistedGrantStore = persistedGrantStore;
        _backChannelLogoutService = backChannelLogoutService;
    }

    /// <inheritdoc/>
    public Task<QueryResult<UserSession>> QuerySessionsAsync(SessionQuery filter = null, CancellationToken cancellationToken = default)
    {
        using var activity = Tracing.ServiceActivitySource.StartActivity("DefaultSessionManagementService.QuerySessions");

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
        using var activity = Tracing.ServiceActivitySource.StartActivity("DefaultSessionManagementService.RemoveSessions");

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
            var sessions = await _serverSideTicketStore.GetSessionsAsync(
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
}
