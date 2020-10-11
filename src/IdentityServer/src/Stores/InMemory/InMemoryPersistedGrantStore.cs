// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Duende.IdentityServer.Stores
{
    /// <summary>
    /// In-memory persisted grant store
    /// </summary>
    public class InMemoryPersistedGrantStore : IPersistedGrantStore
    {
        private readonly ConcurrentDictionary<string, PersistedGrant> _repository = new ConcurrentDictionary<string, PersistedGrant>();

        /// <inheritdoc/>
        public Task StoreAsync(PersistedGrant grant)
        {
            _repository[grant.Key] = grant;

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<PersistedGrant> GetAsync(string key)
        {
            if (_repository.TryGetValue(key, out PersistedGrant token))
            {
                return Task.FromResult(token);
            }

            return Task.FromResult<PersistedGrant>(null);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
        {
            filter.Validate();
            
            var items = Filter(filter);
            
            return Task.FromResult(items);
        }

        /// <inheritdoc/>
        public Task RemoveAsync(string key)
        {
            _repository.TryRemove(key, out _);

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveAllAsync(PersistedGrantFilter filter)
        {
            filter.Validate();

            var items = Filter(filter);
            
            foreach (var item in items)
            {
                _repository.TryRemove(item.Key, out _);
            }

            return Task.CompletedTask;
        }

        private IEnumerable<PersistedGrant> Filter(PersistedGrantFilter filter)
        {
            var query =
                from item in _repository
                select item.Value;

            if (!String.IsNullOrWhiteSpace(filter.ClientId))
            {
                query = query.Where(x => x.ClientId == filter.ClientId);
            }
            if (!String.IsNullOrWhiteSpace(filter.SessionId))
            {
                query = query.Where(x => x.SessionId == filter.SessionId);
            }
            if (!String.IsNullOrWhiteSpace(filter.SubjectId))
            {
                query = query.Where(x => x.SubjectId == filter.SubjectId);
            }
            if (!String.IsNullOrWhiteSpace(filter.Type))
            {
                query = query.Where(x => x.Type == filter.Type);
            }

            var items = query.ToArray().AsEnumerable();
            return items;
        }
    }
}