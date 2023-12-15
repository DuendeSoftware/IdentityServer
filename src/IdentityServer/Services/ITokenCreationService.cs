// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

using Duende.IdentityServer.Models;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Services;

/// <summary>
/// Logic for creating security tokens
/// </summary>
public interface ITokenCreationService
{
    /// <summary>
    /// Creates a token.
    /// </summary>
    /// <param name="token">The token description.</param>
    /// <returns>A protected and serialized security token</returns>
    Task<string> CreateTokenAsync(Token token);
}
