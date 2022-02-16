// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace Duende.SessionManagement;

/// <summary>
/// IUserSession-backed ticket store
/// </summary>
public class ServerSideTicketStore : IServerSideTicketStore
{
    private readonly IdentityServerOptions _options;
    private readonly IUserSessionStore _store;
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
        IUserSessionStore store,
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

        var name = String.IsNullOrWhiteSpace(_options.Authentication.UserDisplayNameClaimType) ? null : ticket.Principal.FindFirst(_options.Authentication.UserDisplayNameClaimType)?.Value;

        var session = new UserSession
        {
            Key = key,
            Scheme = ticket.AuthenticationScheme,
            Created = ticket.GetIssued(),
            Renewed = ticket.GetIssued(),
            Expires = ticket.GetExpiration(),
            SubjectId = ticket.GetSubjectId(),
            SessionId = ticket.GetSessionId(),
            DisplayName = name,
            Ticket = ticket.Serialize(_protector)
        };

        await _store.CreateUserSessionAsync(session);

        return key;
    }

    /// <inheritdoc />
    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        
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
        ArgumentNullException.ThrowIfNull(ticket);
        
        var session = await _store.GetUserSessionAsync(key);
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

        await _store.UpdateUserSessionAsync(session);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        
        _logger.LogDebug("Removing AuthenticationTicket from store for key {key}", key);

        return _store.DeleteUserSessionAsync(key);
    }
}
