// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Hosting.DynamicProviders
{
    class NopIdentityProviderStore : IIdentityProviderStore
    {
        public Task<IEnumerable<IdentityProviderName>> GetAllSchemeNamesAsync()
        {
            return Task.FromResult(Enumerable.Empty<IdentityProviderName>());
        }

        public Task<IdentityProvider> GetBySchemeAsync(string scheme)
        {
            return Task.FromResult<IdentityProvider>(null);
        }
    }
}
