// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace Duende.SessionManagement;

/// <summary>
/// IUserSession-backed ticket store
/// </summary>
public class ServerSideTicketStore : IServerTicketStore
{
    private readonly IUserSessionStore _store;
    private readonly IDataProtector _protector;
    private readonly ILogger<ServerSideTicketStore> _logger;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="store"></param>
    /// <param name="dataProtectionProvider"></param>
    /// <param name="logger"></param>
    public ServerSideTicketStore(
        IUserSessionStore store,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<ServerSideTicketStore> logger)
    {
        _store = store;
        _protector = dataProtectionProvider.CreateProtector("Duende.Bff.ServerSideTicketStore");
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        // TODO: do we need this delete?
        //// it's possible that the user re-triggered OIDC (somehow) prior to
        //// the session DB records being cleaned up, so we should preemptively remove
        //// conflicting session records for this sub/sid combination
        //await _store.DeleteUserSessionsAsync(new UserSessionsFilter
        //{
        //    SubjectId = ticket.GetSubjectId(),
        //    SessionId = ticket.GetSessionId()
        //});

        var key = CryptoRandom.CreateUniqueId(format: CryptoRandom.OutputFormat.Hex);

        _logger.LogDebug("Creating entry in store for AuthenticationTicket, key {key}, with expiration: {expiration}", key, ticket.GetExpiration());

        var session = new UserSession
        {
            Key = key,
            Created = ticket.GetIssued(),
            Renewed = ticket.GetIssued(),
            Expires = ticket.GetExpiration(),
            SubjectId = ticket.GetSubjectId(),
            SessionId = ticket.GetSessionId(),
            Ticket = ticket.Serialize(_protector)
        };

        await _store.CreateUserSessionAsync(session);

        return key;
    }

    /// <inheritdoc />
    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        _logger.LogDebug("Retrieve AuthenticationTicket for key {key}", key);

        var session = await _store.GetUserSessionAsync(key);
        if (session == null)
        {
            _logger.LogDebug("No ticket found in store for {key}", key);
            return null;
        }

        var ticket = session.Deserialize(_protector, _logger);
        if (ticket != null)
        {
            _logger.LogDebug("Ticket loaded for key: {key}, with expiration: {expiration}", key, ticket.GetExpiration());
            return ticket;
        }

        // if we failed to get a ticket, then remove DB record 
        _logger.LogWarning("Failed to deserialize authentication ticket from store, deleting record for key {key}", key);
        await RemoveAsync(key);

        return ticket;
    }

    /// <inheritdoc />
    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var session = await _store.GetUserSessionAsync(key);
        if (session == null)
        {
            throw new InvalidOperationException($"No matching item in store for key `{key}`");
        }

        _logger.LogDebug("Renewing AuthenticationTicket for key {key}, with expiration: {expiration}", key, ticket.GetExpiration());

        var sub = ticket.GetSubjectId();
        var sid = ticket.GetSessionId();
        
        var isNew = session.SubjectId != sub || session.SessionId != sid;
        if (isNew)
        {
            session.Created = ticket.GetIssued();
            session.SubjectId = sub;
            session.SessionId = sid;
        }

        session.Renewed = ticket.GetIssued();
        session.Expires = ticket.GetExpiration();
        session.Ticket = ticket.Serialize(_protector);

        await _store.UpdateUserSessionAsync(session);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key)
    {
        _logger.LogDebug("Removing AuthenticationTicket from store for key {key}", key);

        return _store.DeleteUserSessionAsync(key);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<AuthenticationTicket>> GetUserTicketsAsync(UserSessionsFilter filter, CancellationToken cancellationToken)
    {
        var list = new List<AuthenticationTicket>();

        var sessions = await _store.GetUserSessionsAsync(filter, cancellationToken);
        foreach (var session in sessions)
        {
            var ticket = session.Deserialize(_protector, _logger);
            if (ticket != null)
            {
                list.Add(ticket);
            }
            else
            {
                // if we failed to get a ticket, then remove DB record 
                _logger.LogWarning("Failed to deserialize authentication ticket from store, deleting record for key {key}", session.Key);
                await RemoveAsync(session.Key);
            }
        }

        return list;
    }
}
