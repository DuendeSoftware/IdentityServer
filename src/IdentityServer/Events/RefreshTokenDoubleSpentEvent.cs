// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Events;

/// <summary>
/// Event for double spent refresh token
/// </summary>
/// <seealso cref="Event" />
public class RefreshTokenDoubleSpentEvent : Event
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentGrantedEvent" /> class.
    /// </summary>
    /// <param name="subjectId">The subject identifier.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="refreshTokenHandle">The consumed refresh token handle.</param>
    public RefreshTokenDoubleSpentEvent(string subjectId, string refreshTokenHandle, string clientId)
        : base(EventCategories.Error,
            "Refresh token misused",
            EventTypes.Warning,
            EventIds.TokenMissuseEvent)
    {
        SubjectId = subjectId;
        ClientId = clientId;
        RefreshTokenHandle = refreshTokenHandle;
    }

    /// <summary>
    /// Gets or sets the subject identifier.
    /// </summary>
    /// <value>
    /// The subject identifier.
    /// </value>
    public string SubjectId { get; set; }

    /// <summary>
    /// Gets or sets the client ID.
    /// </summary>
    /// <value>
    /// The client identifier.
    /// </value>
    public string ClientId { get; set; }


    /// <summary>
    /// Consumed Refresh Token Handle
    /// </summary>
    /// <value>
    /// RefreshTokenHandle
    /// </value>
    public string RefreshTokenHandle { get; }
}