// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Extensions;
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
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Extracts a subject identifier
    /// </summary>
    public static string GetSubjectId(this AuthenticationTicket ticket)
    {
        return ticket.Principal.FindFirst(JwtClaimTypes.Subject)?.Value ??
               throw new InvalidOperationException("Missing subject id for principal in authentication ticket.");
    }

    /// <summary>
    /// Extracts the session ID
    /// </summary>
    public static string GetSessionId(this AuthenticationTicket ticket)
    {
        return ticket.Properties.GetSessionId() ??
            throw new InvalidOperationException("Missing session id for principal in authentication ticket.");
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
            AuthenticationType = principal.Identity!.AuthenticationType!,
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

        var payload = JsonSerializer.Serialize(data, JsonOptions);
        payload = protector.Protect(payload);

        var envelope = new Envelope { Version = 1, Payload = payload };
        var value = JsonSerializer.Serialize(envelope, JsonOptions);

        return value;
    }

    /// <summary>
    /// Deserializes a UserSession's Ticket to an AuthenticationTicket
    /// </summary>
    public static AuthenticationTicket? Deserialize(this UserSession session, IDataProtector protector, ILogger logger)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<Envelope>(session.Ticket, JsonOptions);
            if (envelope == null)
            {
                return null;
            }

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

            var ticket = JsonSerializer.Deserialize<AuthenticationTicketLite>(payload, JsonOptions);
            if (ticket == null)
            {
                return null;
            }

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
        public string Scheme { get; init; } = default!;

        /// <summary>
        /// The user
        /// </summary>
        public ClaimsPrincipalLite User { get; init; } = default!;

        /// <summary>
        /// The items
        /// </summary>
        public IDictionary<string, string?> Items { get; init; } = default!;
    }

    /// <summary>
    /// Serialization friendly claim
    /// </summary>
    public class ClaimLite
    {
        /// <summary>
        /// The type
        /// </summary>
        public string Type { get; init; } = default!;

        /// <summary>
        /// The value
        /// </summary>
        public string Value { get; init; } = default!;

        /// <summary>
        /// The value type
        /// </summary>
        public string? ValueType { get; init; } = default!;
    }

    /// <summary>
    /// Serialization friendly ClaimsPrincipal
    /// </summary>
    public class ClaimsPrincipalLite
    {
        /// <summary>
        /// The authentication type
        /// </summary>
        public string AuthenticationType { get; init; } = default!;

        /// <summary>
        /// The name claim type
        /// </summary>
        public string NameClaimType { get; init; } = default!;

        /// <summary>
        /// The role claim type
        /// </summary>
        public string RoleClaimType { get; init; } = default!;

        /// <summary>
        /// The claims
        /// </summary>
        public ClaimLite[] Claims { get; init; } = default!;
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
        public string Payload { get; init; } = default!;
    }
}
