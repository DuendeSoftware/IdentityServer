// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Stores
{
    /// <summary>
    /// Interface to model storage of identity providers.
    /// </summary>
    public interface IIdentityProviderStore
    {
        /// <summary>
        /// Gets all identity providers name.
        /// </summary>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        Task<IEnumerable<IdentityProviderName>> GetAllSchemeNamesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the identity provider by scheme name.
        /// </summary>
        /// <param name="scheme"></param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        Task<IdentityProvider> GetBySchemeAsync(string scheme, CancellationToken cancellationToken = default);
    }
}
