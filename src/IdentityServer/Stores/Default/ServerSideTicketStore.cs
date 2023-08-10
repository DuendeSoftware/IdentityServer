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
/// IServerSideSessionService backed by server side session store
/// </summary>
public class ServerSideTicketStore : IServerSideTicketStore
{
    private readonly IdentityServerOptions _options;
    private readonly IIssuerNameService _issuerNameService;
    private readonly IServerSideSessionStore _store;
    private readonly IDataProtector _protector;
    private readonly ILogger<ServerSideTicketStore> _logger;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="issuerNameService"></param>
    /// <param name="store"></param>
    /// <param name="dataProtectionProvider"></param>
    /// <param name="logger"></param>
    public ServerSideTicketStore(
        IdentityServerOptions options,
        IIssuerNameService issuerNameService,
        IServerSideSessionStore store,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<ServerSideTicketStore> logger)
    {
        _options = options;
        _issuerNameService = issuerNameService;
        _store = store;
        _protector = dataProtectionProvider.CreateProtector("Duende.SessionManagement.ServerSideTicketStore");
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("ServerSideTicketStore.Store");

        ArgumentNullException.ThrowIfNull(ticket);

        ticket.SetIssuer(await _issuerNameService.GetCurrentAsync());

        var key = CryptoRandom.CreateUniqueId(format: CryptoRandom.OutputFormat.Hex);

        await CreateNewSessionAsync(key, ticket);

        return key;
    }

    private async Task CreateNewSessionAsync(string key, AuthenticationTicket ticket)
    {
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
            DisplayName = ticket.GetDisplayName(_options.ServerSideSessions.UserDisplayNameClaimType),
            Ticket = ticket.Serialize(_protector)
        };

        await _store.CreateSessionAsync(session);
    }

    /// <inheritdoc />
    public async Task<AuthenticationTicket> RetrieveAsync(string key)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("ServerSideTicketStore.Retrieve");
        
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
        using var activity = Tracing.StoreActivitySource.StartActivity("ServerSideTicketStore.Renew");
        
        ArgumentNullException.ThrowIfNull(ticket);

        var session = await _store.GetSessionAsync(key);
        if (session == null)
        {
            // https://github.com/dotnet/aspnetcore/issues/41516#issuecomment-1178076544
            await CreateNewSessionAsync(key, ticket);
            return;
        }

        _logger.LogDebug("Renewing AuthenticationTicket for key {key}, with expiration: {expiration}", key, ticket.GetExpiration());

        var sub = ticket.GetSubjectId();
        var sid = ticket.GetSessionId();
        var name = String.IsNullOrWhiteSpace(_options.ServerSideSessions.UserDisplayNameClaimType) ? null : ticket.Principal.FindFirst(_options.ServerSideSessions.UserDisplayNameClaimType)?.Value;

        var isNew = session.SubjectId != sub || session.SessionId != sid;
        if (isNew)
        {
            session.Created = ticket.GetIssued();
            session.SubjectId = sub;
            session.SessionId = sid;
        }

        if (ticket.GetIssuer() == null)
        {
            // when issuing a new cookie on top of an existing cookie, the AuthenticationTicket passed above is new (and not the prior one loaded from the ticket store)
            ticket.SetIssuer(await _issuerNameService.GetCurrentAsync());
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
        using var activity = Tracing.StoreActivitySource.StartActivity("ServerSideTicketStore.Remove");
        
        ArgumentNullException.ThrowIfNull(key);

        _logger.LogDebug("Removing AuthenticationTicket from store for key {key}", key);

        return _store.DeleteSessionAsync(key);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<UserSession>> GetSessionsAsync(SessionFilter filter, CancellationToken cancellationToken = default)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("ServerSideTicketStore.GetSessions");
        
        var sessions = await _store.GetSessionsAsync(filter, cancellationToken);

        return AsUserSessions(sessions);

    }

    /// <inheritdoc />
    public async Task<QueryResult<UserSession>> QuerySessionsAsync(SessionQuery filter = null, CancellationToken cancellationToken = default)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("ServerSideTicketStore.QuerySessions");
        
        var results = await _store.QuerySessionsAsync(filter, cancellationToken);

        var tickets = AsUserSessions(results.Results);

        var result = new QueryResult<UserSession>
        {
            ResultsToken = results.ResultsToken,
            HasPrevResults = results.HasPrevResults,
            HasNextResults = results.HasNextResults,
            TotalCount = results.TotalCount,
            TotalPages = results.TotalPages,
            CurrentPage = results.CurrentPage,
            Results = tickets.ToArray(),
        };

        return result;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<UserSession>> GetAndRemoveExpiredSessionsAsync(int count, CancellationToken cancellationToken = default)
    {
        using var activity = Tracing.StoreActivitySource.StartActivity("ServerSideTicketStore.GetAndRemoveExpiredSessions");
        
        var sessions = await _store.GetAndRemoveExpiredSessionsAsync(count, cancellationToken);

        return AsUserSessions(sessions);
    }

    private UserSession[] AsUserSessions(IEnumerable<ServerSideSession> sessions)
    {
        return sessions
            .Select(x => new { x.Created, Ticket = x.Deserialize(_protector, _logger)! })
            .Where(x => x != null && x.Ticket != null)
            .Select(item => new UserSession
            {
                SubjectId = item.Ticket.GetSubjectId(),
                SessionId = item.Ticket.GetSessionId(),
                DisplayName = item.Ticket.GetDisplayName(_options.ServerSideSessions.UserDisplayNameClaimType),
                Created = item.Created,
                Renewed = item.Ticket.GetIssued(),
                Expires = item.Ticket.GetExpiration(),
                Issuer = item.Ticket.GetIssuer(),
                ClientIds = item.Ticket.Properties.GetClientList().ToList().AsReadOnly(),
                AuthenticationTicket = item.Ticket
            })
            .ToArray();
    }
}
