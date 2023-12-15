// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

using System.Threading.Tasks;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Services;

/// <summary>
/// Logic for creating security tokens
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates an identity token.
    /// </summary>
    /// <param name="request">The token creation request.</param>
    /// <returns>An identity token</returns>
    Task<Token> CreateIdentityTokenAsync(TokenCreationRequest request);

    /// <summary>
    /// Creates an access token.
    /// </summary>
    /// <param name="request">The token creation request.</param>
    /// <returns>An access token</returns>
    Task<Token> CreateAccessTokenAsync(TokenCreationRequest request);

    /// <summary>
    /// Creates a serialized and protected security token.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>A security token in serialized form</returns>
    Task<string> CreateSecurityTokenAsync(Token token);
}
