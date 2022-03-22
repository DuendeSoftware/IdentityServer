// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Services;

/// <summary>
/// Service responsible for logic around coordinating client and server session lifetimes.
/// </summary>
public interface ISessionCoordinationService
{
    /// <summary>
    /// Coordinates when a user logs out.
    /// </summary>
    Task ProcessLogoutAsync(UserSession session);
    
    /// <summary>
    /// Coordinates when a user session has expired.
    /// </summary>
    Task ProcessExpirationAsync(UserSession session);

    /// <summary>
    /// Validates refresh token request against server-side session.
    /// </summary>
    Task<TokenValidationResult> ValidateRefreshTokenAsync(TokenValidationResult result);
    
    /// <summary>
    /// Extends the server-side session for the refresh token being updated.
    /// </summary>
    Task ProcessRefreshTokenUpdateAsync(RefreshTokenUpdateRequest request);
}
