// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Services
{
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
}