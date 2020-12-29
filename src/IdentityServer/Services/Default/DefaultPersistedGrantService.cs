// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Stores.Serialization;
using System.Collections.Generic;
using System.Linq;
using System;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Services
{
    /// <summary>
    /// Default persisted grant service
    /// </summary>
    public class DefaultPersistedGrantService : IPersistedGrantService
    {
        private readonly ILogger _logger;
        private readonly IPersistedGrantStore _store;
        private readonly IPersistentGrantSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultPersistedGrantService"/> class.
        /// </summary>
        /// <param name="store">The store.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="logger">The logger.</param>
        public DefaultPersistedGrantService(IPersistedGrantStore store, 
            IPersistentGrantSerializer serializer,
            ILogger<DefaultPersistedGrantService> logger)
        {
            _store = store;
            _serializer = serializer;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Grant>> GetAllGrantsAsync(string subjectId)
        {
            if (String.IsNullOrWhiteSpace(subjectId)) throw new ArgumentNullException(nameof(subjectId));
            
            var grants = (await _store.GetAllAsync(new PersistedGrantFilter { SubjectId = subjectId })).ToArray();

            try
            {
                var consents = grants.Where(x => x.Type == IdentityServerConstants.PersistedGrantTypes.UserConsent)
                    .Select(x => _serializer.Deserialize<Consent>(x.Data))
                    .Select(x => new Grant 
                    {
                        ClientId = x.ClientId,
                        SubjectId = subjectId,
                        Scopes = x.Scopes,
                        CreationTime = x.CreationTime,
                        Expiration = x.Expiration
                    });

                var codes = grants.Where(x => x.Type == IdentityServerConstants.PersistedGrantTypes.AuthorizationCode)
                    .Select(x => _serializer.Deserialize<AuthorizationCode>(x.Data))
                    .Select(x => new Grant
                    {
                        ClientId = x.ClientId,
                        SubjectId = subjectId,
                        Description = x.Description,
                        Scopes = x.RequestedScopes,
                        CreationTime = x.CreationTime,
                        Expiration = x.CreationTime.AddSeconds(x.Lifetime)
                    });

                var refresh = grants.Where(x => x.Type == IdentityServerConstants.PersistedGrantTypes.RefreshToken)
                    .Select(x => _serializer.Deserialize<RefreshToken>(x.Data))
                    .Select(x => new Grant
                    {
                        ClientId = x.ClientId,
                        SubjectId = subjectId,
                        Description = x.Description,
                        Scopes = x.AuthorizedScopes,
                        CreationTime = x.CreationTime,
                        Expiration = x.CreationTime.AddSeconds(x.Lifetime)
                    });

                var access = grants.Where(x => x.Type == IdentityServerConstants.PersistedGrantTypes.ReferenceToken)
                    .Select(x => _serializer.Deserialize<Token>(x.Data))
                    .Select(x => new Grant
                    {
                        ClientId = x.ClientId,
                        SubjectId = subjectId,
                        Description = x.Description,
                        Scopes = x.Scopes,
                        CreationTime = x.CreationTime,
                        Expiration = x.CreationTime.AddSeconds(x.Lifetime)
                    });

                consents = Join(consents, codes);
                consents = Join(consents, refresh);
                consents = Join(consents, access);

                return consents.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed processing results from grant store.");
            }

            return Enumerable.Empty<Grant>();
        }

        private IEnumerable<Grant> Join(IEnumerable<Grant> first, IEnumerable<Grant> second)
        {
            var list = first.ToList();

            foreach(var other in second)
            {
                var match = list.FirstOrDefault(x => x.ClientId == other.ClientId);
                if (match != null)
                {
                    match.Scopes = match.Scopes.Union(other.Scopes).Distinct();

                    if (match.CreationTime > other.CreationTime)
                    {
                        // show the earlier creation time
                        match.CreationTime = other.CreationTime;
                    }

                    if (match.Expiration == null || other.Expiration == null)
                    {
                        // show that there is no expiration to one of the grants
                        match.Expiration = null;
                    }
                    else if (match.Expiration < other.Expiration)
                    {
                        // show the latest expiration
                        match.Expiration = other.Expiration;
                    }

                    match.Description = match.Description ?? other.Description;
                }
                else
                {
                    list.Add(other);
                }
            }

            return list;
        }

        /// <inheritdoc/>
        public Task RemoveAllGrantsAsync(string subjectId, string clientId = null, string sessionId = null)
        {
            if (String.IsNullOrWhiteSpace(subjectId)) throw new ArgumentNullException(nameof(subjectId));

            return _store.RemoveAllAsync(new PersistedGrantFilter {
                SubjectId = subjectId,
                ClientId = clientId,
                SessionId = sessionId
            });
        }
    }
}