// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Services
{
    /// <summary>
    /// Default handle generation service
    /// </summary>
    /// <seealso cref="IHandleGenerationService" />
    public class DefaultHandleGenerationService : IHandleGenerationService
    {
        /// <summary>
        /// Generates a handle.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        public Task<string> GenerateAsync(int length)
        {
            return Task.FromResult(CryptoRandom.CreateUniqueId(length, CryptoRandom.OutputFormat.Hex));
        }
    }
}