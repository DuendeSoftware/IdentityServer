// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores.Serialization;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Stores
{
    /// <summary>
    /// Default refresh token store.
    /// </summary>
    public class DefaultRefreshTokenStore : DefaultGrantStore<RefreshToken>, IRefreshTokenStore
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRefreshTokenStore"/> class.
        /// </summary>
        /// <param name="store">The store.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="handleGenerationService">The handle generation service.</param>
        /// <param name="logger">The logger.</param>
        public DefaultRefreshTokenStore(
            IPersistedGrantStore store, 
            IPersistentGrantSerializer serializer, 
            IHandleGenerationService handleGenerationService,
            ILogger<DefaultRefreshTokenStore> logger) 
            : base(IdentityServerConstants.PersistedGrantTypes.RefreshToken, store, serializer, handleGenerationService, logger)
        {
        }

        /// <inheritdoc/>
        public async Task<string> StoreRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            return await CreateItemAsync(refreshToken, refreshToken.ClientId, refreshToken.SubjectId, refreshToken.SessionId, refreshToken.Description, refreshToken.CreationTime, refreshToken.Lifetime, cancellationToken);
        }

        /// <inheritdoc/>
        public Task UpdateRefreshTokenAsync(string handle, RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            return StoreItemAsync(handle, refreshToken, refreshToken.ClientId, refreshToken.SubjectId, refreshToken.SessionId, refreshToken.Description, refreshToken.CreationTime, refreshToken.CreationTime.AddSeconds(refreshToken.Lifetime), refreshToken.ConsumedTime, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<RefreshToken> GetRefreshTokenAsync(string refreshTokenHandle, CancellationToken cancellationToken = default)
        {
            var refreshToken = await GetItemAsync(refreshTokenHandle, cancellationToken);

            if (refreshToken != null && refreshToken.Version < 5)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var user = new IdentityServerUser(refreshToken.AccessToken.SubjectId);
                if (refreshToken.AccessToken.Claims != null)
                {
                    foreach (var claim in refreshToken.AccessToken.Claims)
                    {
                        user.AdditionalClaims.Add(claim);
                    }
                }

                refreshToken.Subject = user.CreatePrincipal();
                refreshToken.ClientId = refreshToken.AccessToken.ClientId;
                refreshToken.Description = refreshToken.AccessToken.Description;
                refreshToken.AuthorizedScopes = refreshToken.AccessToken.Scopes;
                refreshToken.SetAccessToken(refreshToken.AccessToken);
                refreshToken.AccessToken = null;
                refreshToken.Version = 5;
#pragma warning restore CS0618 // Type or member is obsolete
            }

            return refreshToken;
        }

        /// <inheritdoc/>
        public Task RemoveRefreshTokenAsync(string refreshTokenHandle, CancellationToken cancellationToken = default)
        {
            return RemoveItemAsync(refreshTokenHandle, cancellationToken);
        }

        /// <inheritdoc/>
        public Task RemoveRefreshTokensAsync(string subjectId, string clientId, CancellationToken cancellationToken = default)
        {
            return RemoveAllAsync(subjectId, clientId, cancellationToken);
        }
    }
}