// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores.Serialization;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Stores
{
    /// <summary>
    /// Default reference token store.
    /// </summary>
    public class DefaultReferenceTokenStore : DefaultGrantStore<Token>, IReferenceTokenStore
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultReferenceTokenStore"/> class.
        /// </summary>
        /// <param name="store">The store.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="handleGenerationService">The handle generation service.</param>
        /// <param name="logger">The logger.</param>
        public DefaultReferenceTokenStore(
            IPersistedGrantStore store, 
            IPersistentGrantSerializer serializer,
            IHandleGenerationService handleGenerationService,
            ILogger<DefaultReferenceTokenStore> logger) 
            : base(IdentityServerConstants.PersistedGrantTypes.ReferenceToken, store, serializer, handleGenerationService, logger)
        {
        }

        /// <summary>
        /// Stores the reference token asynchronous.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        public Task<string> StoreReferenceTokenAsync(Token token)
        {
            return CreateItemAsync(token, token.ClientId, token.SubjectId, token.SessionId, token.Description, token.CreationTime, token.Lifetime);
        }

        /// <summary>
        /// Gets the reference token asynchronous.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <returns></returns>
        public Task<Token> GetReferenceTokenAsync(string handle)
        {
            return GetItemAsync(handle);
        }

        /// <summary>
        /// Removes the reference token asynchronous.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <returns></returns>
        public Task RemoveReferenceTokenAsync(string handle)
        {
            return RemoveItemAsync(handle);
        }

        /// <summary>
        /// Removes the reference tokens asynchronous.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <returns></returns>
        public Task RemoveReferenceTokensAsync(string subjectId, string clientId)
        {
            return RemoveAllAsync(subjectId, clientId);
        }
    }
}