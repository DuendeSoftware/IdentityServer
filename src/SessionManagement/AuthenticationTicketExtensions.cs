// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Duende.SessionManagement;

/// <summary>
///  Extension methods for AuthenticationTicket
/// </summary>
public static class AuthenticationTicketExtensions
{
    static readonly JsonSerializerOptions _jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Extracts a subject identifier
    /// </summary>
    public static string? GetSubjectId(this AuthenticationTicket ticket)
    {
        return ticket.Principal.FindFirst(JwtClaimTypes.Subject)?.Value ??
               ticket.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               // for the mfa remember me cookie, ASP.NET Identity uses the 'name' claim for the subject id (for some reason)
               ticket.Principal.FindFirst(ClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Extracts the session ID
    /// </summary>
    public static string? GetSessionId(this AuthenticationTicket ticket)
    {
        return ticket.Principal.FindFirst(JwtClaimTypes.SessionId)?.Value;
    }

    /// <summary>
    /// Extracts the issuance time
    /// </summary>
    public static DateTime GetIssued(this AuthenticationTicket ticket)
    {
        return ticket.Properties.IssuedUtc?.UtcDateTime ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Extracts the expiration time
    /// </summary>
    public static DateTime? GetExpiration(this AuthenticationTicket ticket)
    {
        return ticket.Properties.ExpiresUtc?.UtcDateTime;
    }

    /// <summary>
    /// Converts a ClaimsPrincipalLite to ClaimsPrincipal
    /// </summary>
    private static ClaimsPrincipal ToClaimsPrincipal(this ClaimsPrincipalLite principal)
    {
        var claims = principal.Claims.Select(x => new Claim(x.Type, x.Value, x.ValueType ?? ClaimValueTypes.String)).ToArray();
        var id = new ClaimsIdentity(claims, principal.AuthenticationType, principal.NameClaimType, principal.RoleClaimType);

        return new ClaimsPrincipal(id);
    }

    /// <summary>
    /// Converts a ClaimsPrincipal to ClaimsPrincipalLite
    /// </summary>
    private static ClaimsPrincipalLite ToClaimsPrincipalLite(this ClaimsPrincipal principal)
    {
        var claims = principal.Claims.Select(
                x => new ClaimLite
                {
                    Type = x.Type,
                    Value = x.Value,
                    ValueType = x.ValueType == ClaimValueTypes.String ? null : x.ValueType
                }).ToArray();

        return new ClaimsPrincipalLite
        {
            AuthenticationType = principal.Identity.AuthenticationType,
            NameClaimType = principal.Identities.First().NameClaimType,
            RoleClaimType = principal.Identities.First().RoleClaimType,
            Claims = claims
        };
    }

    /// <summary>
    /// Serializes and AuthenticationTicket to a string
    /// </summary>
    public static string Serialize(this AuthenticationTicket ticket, IDataProtector protector)
    {
        var data = new AuthenticationTicketLite
        {
            Scheme = ticket.AuthenticationScheme,
            User = ticket.Principal.ToClaimsPrincipalLite(),
            Items = ticket.Properties.Items
        };

        var payload = JsonSerializer.Serialize(data, _jsonOptions);
        payload = protector.Protect(payload);

        var envelope = new Envelope { Version = 1, Payload = payload };
        var value = JsonSerializer.Serialize(envelope, _jsonOptions);

        return value;
    }

    /// <summary>
    /// Deserializes a UserSession's Ticket to an AuthenticationTicket
    /// </summary>
    public static AuthenticationTicket Deserialize(this UserSession session, IDataProtector protector, ILogger logger)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<Envelope>(session.Ticket, _jsonOptions);
            if (envelope.Version != 1)
            {
                logger.LogWarning("Deserializing AuthenticationTicket envelope found incorrect version for key {key}.", session.Key);
                return null;
            }

            string payload;
            try
            {
                payload = protector.Unprotect(envelope.Payload);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to unprotect AuthenticationTicket payload for key {key}", session.Key);
                return null;
            }

            var ticket = JsonSerializer.Deserialize<AuthenticationTicketLite>(payload, _jsonOptions);

            var user = ticket.User.ToClaimsPrincipal();
            var properties = new AuthenticationProperties(ticket.Items);

            // this allows us to extend the session from the DB column rather than from the payload
            if (session.Expires.HasValue)
            {
                properties.ExpiresUtc = new DateTimeOffset(session.Expires.Value, TimeSpan.Zero);
            }
            else
            {
                properties.ExpiresUtc = null;
            }

            return new AuthenticationTicket(user, properties, ticket.Scheme);
        }
        catch (Exception ex)
        {
            // failed deserialize
            logger.LogError(ex, "Failed to deserialize UserSession payload for key {key}", session.Key);
        }

        return null;
    }

    /// <summary>
    /// Serialization friendly AuthenticationTicket
    /// </summary>
    public class AuthenticationTicketLite
    {
        /// <summary>
        /// The scheme
        /// </summary>
        public string Scheme { get; set; }

        /// <summary>
        /// The user
        /// </summary>
        public ClaimsPrincipalLite User { get; set; }

        /// <summary>
        /// The items
        /// </summary>
        public IDictionary<string, string> Items { get; set; }
    }

    /// <summary>
    /// Serialization friendly claim
    /// </summary>
    public class ClaimLite
    {
        /// <summary>
        /// The type
        /// </summary>
        public string Type { get; init; }

        /// <summary>
        /// The value
        /// </summary>
        public string Value { get; init; }

        /// <summary>
        /// The value type
        /// </summary>
        public string ValueType { get; init; }
    }

    /// <summary>
    /// Serialization friendly ClaimsPrincipal
    /// </summary>
    public class ClaimsPrincipalLite
    {
        /// <summary>
        /// The authentication type
        /// </summary>
        public string AuthenticationType { get; init; }

        /// <summary>
        /// The name claim type
        /// </summary>
        public string NameClaimType { get; init; }

        /// <summary>
        /// The role claim type
        /// </summary>
        public string RoleClaimType { get; init; }

        /// <summary>
        /// The claims
        /// </summary>
        public ClaimLite[] Claims { get; init; }
    }

    /// <summary>
    /// Envelope for serialized data
    /// </summary>
    public class Envelope
    {
        /// <summary>
        /// Version
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Payload
        /// </summary>
        public string Payload { get; set; }
    }
}


// todo
//public class TicketCleanupService : IHostedService
//{
//    private readonly IServiceProvider _serviceProvider;
//    private readonly SessionManagementOptions _options;
//    private readonly ILogger<TicketCleanupService> _logger;

//    private CancellationTokenSource _source;

//    public TicketCleanupService(
//        IServiceProvider serviceProvider,
//        SessionManagementOptions options,
//        ILogger<TicketCleanupService> logger)
//    {
//        _serviceProvider = serviceProvider;
//        _options = options;
//        _logger = logger;
//    }

//    public Task StartAsync(CancellationToken cancellationToken)
//    {
//        if (_options.EnableSessionCleanupInterval)
//        {
//            if (_source != null) throw new InvalidOperationException("Already started. Call Stop first.");

//            _logger.LogDebug("Starting ticket cleanup");

//            _source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

//            Task.Factory.StartNew(() => StartInternalAsync(_source.Token));
//        }

//        return Task.CompletedTask;
//    }

//    public Task StopAsync(CancellationToken cancellationToken)
//    {
//        if (_options.EnableSessionCleanupInterval)
//        {
//            if (_source == null) throw new InvalidOperationException("Not started. Call Start first.");

//            _logger.LogDebug("Stopping ticket cleanup");

//            _source.Cancel();
//            _source = null;
//        }

//        return Task.CompletedTask;
//    }

//    private async Task StartInternalAsync(CancellationToken cancellationToken)
//    {
//        while (true)
//        {
//            if (cancellationToken.IsCancellationRequested)
//            {
//                _logger.LogDebug("CancellationRequested. Exiting.");
//                break;
//            }

//            try
//            {
//                await Task.Delay(_options.SessionCleanupInterval, cancellationToken);
//            }
//            catch (TaskCanceledException)
//            {
//                _logger.LogDebug("TaskCanceledException. Exiting.");
//                break;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError("Task.Delay exception: {0}. Exiting.", ex.Message);
//                break;
//            }

//            if (cancellationToken.IsCancellationRequested)
//            {
//                _logger.LogDebug("CancellationRequested. Exiting.");
//                break;
//            }

//            await RemoveExpiredTicketsAsync();
//        }
//    }

//    private async Task RemoveExpiredTicketsAsync()
//    {
//        try
//        {
//            _logger.LogTrace("Querying for expired tickets to remove");

//            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
//            {
//                using (var context = serviceScope.ServiceProvider.GetService<SessionManagementDbContext>())
//                {
//                    await RemoveExpiredTicketsAsync(context);
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError("Exception removing expired tickets: {exception}", ex.Message);
//        }
//    }

//    private async Task RemoveExpiredTicketsAsync(SessionManagementDbContext context)
//    {
//        var found = Int32.MaxValue;

//        while (found >= _options.SessionCleanupBatchSize)
//        {
//            var expiredItems = await context.UserSessions
//                .Where(x => x.Expires < DateTime.UtcNow)
//                .OrderBy(x => x.Id)
//                .Take(_options.SessionCleanupBatchSize)
//                .ToArrayAsync();

//            found = expiredItems.Length;
//            _logger.LogInformation("Removing {expiredItems} tickets", found);

//            if (found > 0)
//            {
//                context.UserSessions.RemoveRange(expiredItems);
//                try
//                {
//                    await context.SaveChangesAsync();
//                }
//                catch (DbUpdateConcurrencyException ex)
//                {
//                    // we get this if/when someone else already deleted the records
//                    // we want to essentially ignore this, and keep working
//                    _logger.LogDebug("Concurrency exception removing expired tickets: {exception}", ex.Message);
//                }
//            }
//        }
//    }
//}
