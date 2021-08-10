// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Microsoft.IdentityModel.Tokens;

namespace Duende.IdentityServer.Services.KeyManagement
{
    /// <summary>
    /// Store abstraction for automatic key management.
    /// </summary>
    public interface IAutomaticKeyManagerKeyStore : IValidationKeysStore, ISigningCredentialStore
    {
        /// <summary>
        /// Gets all the signing credentials.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<SigningCredentials>> GetAllSigningCredentialsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Empty implementation of IAutomaticKeyManagerKeyStore (for testing).
    /// </summary>
    internal class NopAutomaticKeyManagerKeyStore : IAutomaticKeyManagerKeyStore
    {
        /// <inheritdoc/>
        public Task<SigningCredentials> GetSigningCredentialsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<SigningCredentials>(null);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<SigningCredentials>> GetAllSigningCredentialsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Enumerable.Empty<SigningCredentials>());
        }

        /// <inheritdoc/>
        public Task<IEnumerable<SecurityKeyInfo>> GetValidationKeysAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Enumerable.Empty<SecurityKeyInfo>());
        }
    }

    /// <summary>
    /// Implementation of IValidationKeysStore and ISigningCredentialStore based on KeyManager.
    /// </summary>
    public class AutomaticKeyManagerKeyStore : IAutomaticKeyManagerKeyStore
    {
        private readonly IKeyManager _keyManager;
        private readonly KeyManagementOptions _options;

        /// <summary>
        /// Constructor for KeyManagerKeyStore.
        /// </summary>
        /// <param name="keyManager"></param>
        /// <param name="options"></param>
        public AutomaticKeyManagerKeyStore(IKeyManager keyManager, KeyManagementOptions options)
        {
            _keyManager = keyManager;
            _options = options;
        }

        /// <inheritdoc/>
        public async Task<SigningCredentials> GetSigningCredentialsAsync(CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
            {
                return null;
            }

            var credentials = await GetAllSigningCredentialsAsync(cancellationToken);
            var alg = _options.DefaultSigningAlgorithm;
            var credential = credentials.FirstOrDefault(x => alg == x.Algorithm);
            return credential;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SigningCredentials>> GetAllSigningCredentialsAsync(CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
            {
                return Enumerable.Empty<SigningCredentials>();
            }

            var keyContainers = await _keyManager.GetCurrentKeysAsync(cancellationToken);
            var credentials = keyContainers.Select(x => new SigningCredentials(x.ToSecurityKey(), x.Algorithm));
            return credentials;
        }
        
        /// <inheritdoc/>
        public async Task<IEnumerable<SecurityKeyInfo>> GetValidationKeysAsync(CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
            {
                return Enumerable.Empty<SecurityKeyInfo>();
            }
            
            var containers = await _keyManager.GetAllKeysAsync(cancellationToken);
            var keys = containers.Select(x => new SecurityKeyInfo
            {
                Key = x.ToSecurityKey(),
                SigningAlgorithm = x.Algorithm
            });
            return keys.ToArray();
        }
    }
}
