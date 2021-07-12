// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.EntityFramework.Stores
{
    /// <summary>
    /// Implementation of ISigningKeyStore thats uses EF.
    /// </summary>
    /// <seealso cref="ISigningKeyStore" />
    public class SigningKeyStore : ISigningKeyStore
    {
        const string Use = "signing";

        /// <summary>
        /// The DbContext.
        /// </summary>
        protected readonly IPersistedGrantDbContext Context;

        /// <summary>
        /// The logger.
        /// </summary>
        protected readonly ILogger<SigningKeyStore> Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SigningKeyStore"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">context</exception>
        public SigningKeyStore(IPersistedGrantDbContext context, ILogger<SigningKeyStore> logger)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Logger = logger;
        }

        /// <summary>
        /// Loads all keys from store.
        /// </summary>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        public async Task<IEnumerable<SerializedKey>> LoadKeysAsync(CancellationToken cancellationToken)
        {
            var entities = await Context.Keys.Where(x => x.Use == Use).ToArrayAsync(cancellationToken);
            return entities.Select(key => new SerializedKey
            {
                Id = key.Id,
                Created = key.Created,
                Version = key.Version,
                Algorithm = key.Algorithm,
                Data = key.Data,
                DataProtected = key.DataProtected,
                IsX509Certificate = key.IsX509Certificate
            });
        }

        /// <summary>
        /// Persists new key in store.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        public Task StoreKeyAsync(SerializedKey key, CancellationToken cancellationToken)
        {
            var entity = new Key
            {
                Id = key.Id,
                Use = Use,
                Created = key.Created,
                Version = key.Version,
                Algorithm = key.Algorithm,
                Data = key.Data,
                DataProtected = key.DataProtected,
                IsX509Certificate = key.IsX509Certificate
            };
            Context.Keys.Add(entity);
            return Context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Deletes key from storage.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns></returns>
        public async Task DeleteKeyAsync(string id, CancellationToken cancellationToken)
        {
            var item = await Context.Keys.FirstOrDefaultAsync(x => x.Use == Use && x.Id == id, cancellationToken);
            if (item != null)
            {
                try
                {
                    Context.Keys.Remove(item);
                    await Context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    foreach(var entity in ex.Entries)
                    {
                        entity.State = EntityState.Detached;
                    }

                    // already deleted, so we can eat this exception
                    Logger.LogDebug("Concurrency exception caught deleting key id {kid}", id);
                }
            }
        }
    }
}