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
using Duende.IdentityServer.Services.KeyManagement;

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
        private readonly IAutomaticKeyManagerKeyStore _keyManagerKeyStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultKeyMaterialService"/> class.
        /// </summary>
        /// <param name="validationKeysStores">The validation keys stores.</param>
        /// <param name="signingCredentialStores">The signing credential store.</param>
        /// <param name="keyManagerKeyStore">The store for automatic key management.</param>
        public DefaultKeyMaterialService(
            IEnumerable<IValidationKeysStore> validationKeysStores,
            IEnumerable<ISigningCredentialStore> signingCredentialStores,
            IAutomaticKeyManagerKeyStore keyManagerKeyStore)
        {
            _signingCredentialStores = signingCredentialStores;
            _validationKeysStores = validationKeysStores;
            _keyManagerKeyStore = keyManagerKeyStore;
        }

        /// <inheritdoc/>
        public async Task<SigningCredentials> GetSigningCredentialsAsync(IEnumerable<string> allowedAlgorithms = null)
        {
            if (allowedAlgorithms.IsNullOrEmpty())
            {
                var automaticKey = await _keyManagerKeyStore.GetSigningCredentialsAsync();
                if (automaticKey != null)
                {
                    return automaticKey;
                }

                var list = _signingCredentialStores.ToList();
                for (var i = 0; i < list.Count; i++)
                {
                    var key = await list[i].GetSigningCredentialsAsync();
                    if (key != null)
                    {
                        return key;
                    }
                }

                throw new InvalidOperationException($"No signing credential registered.");
            }

            var credential =
                (await GetAllSigningCredentialsAsync()).FirstOrDefault(c => allowedAlgorithms.Contains(c.Algorithm));
            if (credential is null)
            {
                throw new InvalidOperationException(
                    $"No signing credential for algorithms ({allowedAlgorithms.ToSpaceSeparatedString()}) registered.");
            }

            return credential;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SigningCredentials>> GetAllSigningCredentialsAsync()
        {
            var credentials = new List<SigningCredentials>();

            var automaticSigningKeys = await _keyManagerKeyStore.GetAllSigningCredentialsAsync();
            if (automaticSigningKeys != null)
            {
                credentials.AddRange(automaticSigningKeys);
            }

            foreach (var store in _signingCredentialStores)
            {
                var signingKey = await store.GetSigningCredentialsAsync();
                if (signingKey != null)
                {
                    credentials.Add(signingKey);
                }
            }

            return credentials;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SecurityKeyInfo>> GetValidationKeysAsync()
        {
            var keys = new List<SecurityKeyInfo>();

            var automaticSigningKeys = await _keyManagerKeyStore.GetValidationKeysAsync();
            if (automaticSigningKeys?.Any() == true)
            {
                keys.AddRange(automaticSigningKeys);
            }

            foreach (var store in _validationKeysStores)
            {
                var validationKeys = await store.GetValidationKeysAsync();
                if (validationKeys.Any())
                {
                    keys.AddRange(validationKeys);
                }
            }

            return keys;
        }
    }
}