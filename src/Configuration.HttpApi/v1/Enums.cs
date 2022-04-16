// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


namespace Duende.IdentityServer.Configuration.WebApi.v1;

/// <summary>
/// Access token types.
/// </summary>
public enum AccessTokenType
{
    /// <summary>
    /// Self-contained Json Web Token
    /// </summary>
    Jwt,

    /// <summary>
    /// Reference token
    /// </summary>
    Reference
}

/// <summary>
/// Token usage types.
/// </summary>
public enum TokenUsage
{
    /// <summary>
    /// Re-use the refresh token handle
    /// </summary>
    ReUse,

    /// <summary>
    /// Issue a new refresh token handle every time
    /// </summary>
    OneTimeOnly
}

/// <summary>
/// Token expiration types.
/// </summary>
public enum TokenExpiration
{
    /// <summary>
    /// Sliding token expiration
    /// </summary>
    Sliding,

    /// <summary>
    /// Absolute token expiration
    /// </summary>
    Absolute
}