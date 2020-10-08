// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using Duende.IdentityServer.Validation;

namespace Duende.IdentityServer.Services
{
    /// <summary>
    /// Implements refresh token creation and validation
    /// </summary>
    public interface IRefreshTokenService
    {
        /// <summary>
        /// Validates a refresh token.
        /// </summary>
        /// <param name="token">The refresh token.</param>
        /// <param name="client">The client.</param>
        /// <returns></returns>
        Task<TokenValidationResult> ValidateRefreshTokenAsync(string token, Client client);
        
        /// <summary>
        /// Creates the refresh token.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="client">The client.</param>
        /// <returns>
        /// The refresh token handle
        /// </returns>
        Task<string> CreateRefreshTokenAsync(ClaimsPrincipal subject, Token accessToken, Client client);

        /// <summary>
        /// Updates the refresh token.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="refreshToken">The refresh token.</param>
        /// <param name="client">The client.</param>
        /// <returns>
        /// The refresh token handle
        /// </returns>
        Task<string> UpdateRefreshTokenAsync(string handle, RefreshToken refreshToken, Client client);
    }
}