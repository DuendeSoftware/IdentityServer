// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using System;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Extensions;

namespace Duende.IdentityServer.Services
{
    /// <summary>
    /// The default key material service
    /// </summary>
    /// <seealso cref="IKeyMaterialService" />
    public class DefaultKeyMaterialService : IKeyMaterialService
    {
        private readonly IEnumerable<ISigningCredentialStore> _signingCredentialStores;
        private readonly IEnumerable<IValidationKeysStore> _validationKeysStores;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultKeyMaterialService"/> class.
        /// </summary>
        /// <param name="validationKeysStores">The validation keys stores.</param>
        /// <param name="signingCredentialStores">The signing credential store.</param>
        public DefaultKeyMaterialService(IEnumerable<IValidationKeysStore> validationKeysStores, IEnumerable<ISigningCredentialStore> signingCredentialStores)
        {
            _signingCredentialStores = signingCredentialStores;
            _validationKeysStores = validationKeysStores;
        }

        /// <inheritdoc/>
        public async Task<SigningCredentials> GetSigningCredentialsAsync(IEnumerable<string> allowedAlgorithms = null)
        {
            if (_signingCredentialStores.Any())
            {
                if (allowedAlgorithms.IsNullOrEmpty())
                {
                    return await _signingCredentialStores.First().GetSigningCredentialsAsync();
                }

                var credential = (await GetAllSigningCredentialsAsync()).FirstOrDefault(c => allowedAlgorithms.Contains(c.Algorithm));
                if (credential is null)
                {
                    throw new InvalidOperationException($"No signing credential for algorithms ({allowedAlgorithms.ToSpaceSeparatedString()}) registered.");
                }

                return credential;
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SigningCredentials>> GetAllSigningCredentialsAsync()
        {
            var credentials = new List<SigningCredentials>();

            foreach (var store in _signingCredentialStores)
            {
                credentials.Add(await store.GetSigningCredentialsAsync());
            }

            return credentials;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SecurityKeyInfo>> GetValidationKeysAsync()
        {
            var keys = new List<SecurityKeyInfo>();

            foreach (var store in _validationKeysStores)
            {
                keys.AddRange(await store.GetValidationKeysAsync());
            }

            return keys;
        }
    }
}