// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Services;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Stores
{
    /// <summary>
    /// Cache decorator for IClientStore
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="IdentityServer.Stores.IClientStore" />
    public class CachingClientStore<T> : IClientStore
        where T : IClientStore
    {
        private readonly IdentityServerOptions _options;
        private readonly ICache<Client> _cache;
        private readonly IClientStore _inner;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingClientStore{T}"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="inner">The inner.</param>
        /// <param name="cache">The cache.</param>
        /// <param name="logger">The logger.</param>
        public CachingClientStore(IdentityServerOptions options, T inner, ICache<Client> cache, ILogger<CachingClientStore<T>> logger)
        {
            _options = options;
            _inner = inner;
            _cache = cache;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<Client> FindClientByIdAsync(string clientId, CancellationToken cancellationToken)
        {
            var client = await _cache.GetAsync(clientId,
                _options.Caching.ClientStoreExpiration,
                async () => await _inner.FindClientByIdAsync(clientId, cancellationToken),
                _logger);

            return client;
        }
    }
}