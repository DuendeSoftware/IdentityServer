// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Hosting.DynamicProviders
{
    class NopIdentityProviderStore : IIdentityProviderStore
    {
        public Task<IdentityProvider> GetBySchemeAsync(string scheme)
        {
            return Task.FromResult<IdentityProvider>(null);
        }
    }
}
