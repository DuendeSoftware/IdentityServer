// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using IdentityModel;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Duende.IdentityServer.Models;

/// <summary>
/// Models a refresh token.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    /// <value>
    /// The creation time.
    /// </value>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// Gets or sets the life time.
    /// </summary>
    /// <value>
    /// The life time.
    /// </value>
    public int Lifetime { get; set; }

    /// <summary>
    /// Gets or sets the consumed time.
    /// </summary>
    /// <value>
    /// The consumed time.
    /// </value>
    public DateTime? ConsumedTime { get; set; }

    /// <summary>
    /// Obsolete. This property remains to keep backwards compatibility with serialized persisted grants.
    /// </summary>
    /// <value>
    /// The access token.
    /// </value>
    [Obsolete("Use AccessTokens or Set/GetAccessToken instead.")]
    public Token AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the resource indicator specific access token.
    /// </summary>
    /// <value>
    /// The access token.
    /// </value>
    public Dictionary<string, Token> AccessTokens { get; set; } = new Dictionary<string, Token>();

    /// <summary>
    /// Returns the access token based on the resource indicator.
    /// </summary>
    /// <param name="resourceIndicator"></param>
    /// <returns></returns>
    public Token GetAccessToken(string resourceIndicator = null)
    {
        AccessTokens.TryGetValue(resourceIndicator ?? String.Empty, out var token);
        return token;
    }

    /// <summary>
    /// Sets the access token based on the resource indicator.
    /// </summary>
    /// <param name="resourceIndicator"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public void SetAccessToken(Token token, string resourceIndicator = null)
    {
        AccessTokens[resourceIndicator ?? String.Empty] = token;
    }

    /// <summary>
    /// Gets or sets the original subject that requested the token.
    /// </summary>
    /// <value>
    /// The subject.
    /// </value>
    public ClaimsPrincipal Subject { get; set; }

    /// <summary>
    /// Gets or sets the version number.
    /// </summary>
    /// <value>
    /// The version.
    /// </value>
    public int Version { get; set; } = 5;

    /// <summary>
    /// Gets the client identifier.
    /// </summary>
    /// <value>
    /// The client identifier.
    /// </value>
    public string ClientId { get; set; }

    /// <summary>
    /// Gets the subject identifier.
    /// </summary>
    /// <value>
    /// The subject identifier.
    /// </value>
    public string SubjectId => Subject?.FindFirst(JwtClaimTypes.Subject)?.Value;

    /// <summary>
    /// Gets the session identifier.
    /// </summary>
    /// <value>
    /// The session identifier.
    /// </value>
    public string SessionId { get; set; }

    /// <summary>
    /// Gets the description the user assigned to the device being authorized.
    /// </summary>
    /// <value>
    /// The description.
    /// </value>
    public string Description { get; set; }

    /// <summary>
    /// Gets the scopes.
    /// </summary>
    /// <value>
    /// The scopes.
    /// </value>
    public IEnumerable<string> AuthorizedScopes { get; set; }

    /// <summary>
    /// The resource indicators. Null indicates there was no authorization step, thus no restrictions.
    /// Non-null means there was an authorization step, and subsequent requested resource indicators must be in the original list.
    /// </summary>
    public IEnumerable<string> AuthorizedResourceIndicators { get; set; }
}