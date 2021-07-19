// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.EntityFramework.Stores
{
    /// <summary>
    /// Implementation of IClientStore thats uses EF.
    /// </summary>
    /// <seealso cref="IClientStore" />
    public class ClientStore : IClientStore
    {
        /// <summary>
        /// The DbContext.
        /// </summary>
        protected readonly IConfigurationDbContext Context;

        /// <summary>
        /// The logger.
        /// </summary>
        protected readonly ILogger<ClientStore> Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientStore"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">context</exception>
        public ClientStore(IConfigurationDbContext context, ILogger<ClientStore> logger)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Logger = logger;
        }

        /// <summary>
        /// Finds a client by id
        /// </summary>
        /// <param name="clientId">The client id</param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns>
        /// The client
        /// </returns>
        public virtual async Task<Duende.IdentityServer.Models.Client> FindClientByIdAsync(string clientId, CancellationToken cancellationToken = default)
        {
            IQueryable<Duende.IdentityServer.EntityFramework.Entities.Client> baseQuery = Context.Clients
                .Where(x => x.ClientId == clientId);

            var client = (await baseQuery.ToArrayAsync(cancellationToken))
                .SingleOrDefault(x => x.ClientId == clientId);
            if (client == null) return null;

            await baseQuery.Include(x => x.AllowedCorsOrigins).SelectMany(c => c.AllowedCorsOrigins).LoadAsync(cancellationToken);
            await baseQuery.Include(x => x.AllowedGrantTypes).SelectMany(c => c.AllowedGrantTypes).LoadAsync(cancellationToken);
            await baseQuery.Include(x => x.AllowedScopes).SelectMany(c => c.AllowedScopes).LoadAsync(cancellationToken);
            await baseQuery.Include(x => x.Claims).SelectMany(c => c.Claims).LoadAsync(cancellationToken);
            await baseQuery.Include(x => x.ClientSecrets).SelectMany(c => c.ClientSecrets).LoadAsync(cancellationToken);
            await baseQuery.Include(x => x.IdentityProviderRestrictions).SelectMany(c => c.IdentityProviderRestrictions).LoadAsync(cancellationToken);
            await baseQuery.Include(x => x.PostLogoutRedirectUris).SelectMany(c => c.PostLogoutRedirectUris).LoadAsync(cancellationToken);
            await baseQuery.Include(x => x.Properties).SelectMany(c => c.Properties).LoadAsync(cancellationToken);
            await baseQuery.Include(x => x.RedirectUris).SelectMany(c => c.RedirectUris).LoadAsync(cancellationToken);

            var model = client.ToModel();

            Logger.LogDebug("{clientId} found in database: {clientIdFound}", clientId, model != null);

            return model;
        }
    }
}