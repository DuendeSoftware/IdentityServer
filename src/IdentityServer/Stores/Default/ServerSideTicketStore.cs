// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Stores;

/// <summary>
/// IUserSession-backed ticket store
/// </summary>
public class ServerSideTicketStore : IServerSideTicketStore
{
    private readonly IdentityServerOptions _options;
    private readonly IServerSideSessionStore _store;
    private readonly IDataProtector _protector;
    private readonly ILogger<ServerSideTicketStore> _logger;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="store"></param>
    /// <param name="dataProtectionProvider"></param>
    /// <param name="logger"></param>
    public ServerSideTicketStore(
        IdentityServerOptions options,
        IServerSideSessionStore store,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<ServerSideTicketStore> logger)
    {
        _options = options;
        _store = store;
        _protector = dataProtectionProvider.CreateProtector("Duende.SessionManagement.ServerSideTicketStore");
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        var key = CryptoRandom.CreateUniqueId(format: CryptoRandom.OutputFormat.Hex);

        _logger.LogDebug("Creating entry in store for AuthenticationTicket, key {key}, with expiration: {expiration}", key, ticket.GetExpiration());

        var session = new ServerSideSession
        {
            Key = key,
            Scheme = ticket.AuthenticationScheme,
            Created = ticket.GetIssued(),
            Renewed = ticket.GetIssued(),
            Expires = ticket.GetExpiration(),
            SubjectId = ticket.GetSubjectId(),
            SessionId = ticket.GetSessionId(),
            DisplayName = ticket.GetDisplayName(_options.Authentication.UserDisplayNameClaimType),
            Ticket = ticket.Serialize(_protector)
        };

        await _store.CreateSessionAsync(session);

        return key;
    }

    /// <inheritdoc />
    public async Task<AuthenticationTicket> RetrieveAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        _logger.LogDebug("Retrieve AuthenticationTicket for key {key}", key);

        var session = await _store.GetSessionAsync(key);
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
        ArgumentNullException.ThrowIfNull(ticket);

        var session = await _store.GetSessionAsync(key);
        if (session == null)
        {
            throw new InvalidOperationException($"No matching item in store for key `{key}`");
        }

        _logger.LogDebug("Renewing AuthenticationTicket for key {key}, with expiration: {expiration}", key, ticket.GetExpiration());

        var sub = ticket.GetSubjectId();
        var sid = ticket.GetSessionId();
        var name = String.IsNullOrWhiteSpace(_options.Authentication.UserDisplayNameClaimType) ? null : ticket.Principal.FindFirst(_options.Authentication.UserDisplayNameClaimType)?.Value;

        var isNew = session.SubjectId != sub || session.SessionId != sid;
        if (isNew)
        {
            session.Created = ticket.GetIssued();
            session.SubjectId = sub;
            session.SessionId = sid;
        }

        session.Renewed = ticket.GetIssued();
        session.Expires = ticket.GetExpiration();
        session.DisplayName = name;
        session.Ticket = ticket.Serialize(_protector);

        await _store.UpdateSessionAsync(session);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        _logger.LogDebug("Removing AuthenticationTicket from store for key {key}", key);

        return _store.DeleteSessionAsync(key);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<UserSession>> GetSessionsAsync(SessionFilter filter, CancellationToken cancellationToken = default)
    {
        var sessions = await _store.GetSessionsAsync(filter, cancellationToken);

        var results = sessions
            .Select(x => new { x.Renewed, Ticket = x.Deserialize(_protector, _logger)! })
            .Where(x => x != null && x.Ticket != null)
            .Select(item => new UserSession
            {
                SubjectId = item.Ticket.GetSubjectId(),
                SessionId = item.Ticket.GetSessionId(),
                DisplayName = item.Ticket.GetDisplayName(_options.Authentication.UserDisplayNameClaimType),
                Created = item.Ticket.GetIssued(),
                Renewed = item.Renewed,
                Expires = item.Ticket.GetExpiration(),
                ClientIds = item.Ticket.Properties.GetClientList().ToList().AsReadOnly(),
                AuthenticationTicket = item.Ticket
            })
            .ToArray();

        return results;
    }

    /// <inheritdoc />
    public async Task<QueryResult<UserSession>> QuerySessionsAsync(SessionQuery filter = null, CancellationToken cancellationToken = default)
    {
        var results = await _store.QuerySessionsAsync(filter, cancellationToken);

        var tickets = results.Results
            .Select(x => new { x.Renewed, Ticket = x.Deserialize(_protector, _logger)! })
            .Where(x => x != null && x.Ticket != null)
            .Select(item => new UserSession
            {
                SubjectId = item.Ticket.GetSubjectId(),
                SessionId = item.Ticket.GetSessionId(),
                DisplayName = item.Ticket.GetDisplayName(_options.Authentication.UserDisplayNameClaimType),
                Created = item.Ticket.GetIssued(),
                Renewed = item.Renewed,
                Expires = item.Ticket.GetExpiration(),
                ClientIds = item.Ticket.Properties.GetClientList().ToList().AsReadOnly(),
                AuthenticationTicket = item.Ticket
            })
            .ToArray();

        var result = new QueryResult<UserSession>
        {
            CountRequested = results.CountRequested,
            Page = results.Page,
            TotalCount = results.TotalCount,
            TotalPages = results.TotalPages,
            Results = tickets.ToArray(),
        };

        return result;
    }
}
