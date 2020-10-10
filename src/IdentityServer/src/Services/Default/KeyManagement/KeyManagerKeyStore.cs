// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Microsoft.IdentityModel.Tokens;

namespace Duende.IdentityServer.Services.KeyManagement
{
    /// <summary>
    /// Implementation of IValidationKeysStore and ISigningCredentialStore based on KeyManager.
    /// </summary>
    public class KeyManagerKeyStore : IValidationKeysStore, ISigningCredentialStore
    {
        private readonly IKeyManager _keyManager;
        private readonly KeyManagementOptions _options;

        /// <summary>
        /// Constructor for KeyManagerKeyStore.
        /// </summary>
        /// <param name="keyManager"></param>
        /// <param name="options"></param>
        public KeyManagerKeyStore(IKeyManager keyManager, KeyManagementOptions options)
        {
            _keyManager = keyManager;
            _options = options;
        }

        /// <summary>
        /// Returns the current signing key.
        /// </summary>
        /// <returns></returns>
        public async Task<SigningCredentials> GetSigningCredentialsAsync()
        {
            var container = await _keyManager.GetCurrentKeyAsync();
            var key = container.ToSecurityKey();
            var credential = new SigningCredentials(key, GetRsaSigningAlgorithmValue(_options.SigningAlgorithm));
            return credential;
        }

        /// <summary>
        /// Returns all the validation keys.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<SecurityKeyInfo>> GetValidationKeysAsync()
        {
            var containers = await _keyManager.GetAllKeysAsync();
            var keys = containers.Select(x => x.ToSecurityKey());
            return keys.Select(x => new SecurityKeyInfo { Key = x, SigningAlgorithm = GetRsaSigningAlgorithmValue(_options.SigningAlgorithm) });
        }

        internal static string GetRsaSigningAlgorithmValue(IdentityServerConstants.RsaSigningAlgorithm value)
        {
            return value switch
            {
                IdentityServerConstants.RsaSigningAlgorithm.RS256 => SecurityAlgorithms.RsaSha256,
                IdentityServerConstants.RsaSigningAlgorithm.RS384 => SecurityAlgorithms.RsaSha384,
                IdentityServerConstants.RsaSigningAlgorithm.RS512 => SecurityAlgorithms.RsaSha512,

                IdentityServerConstants.RsaSigningAlgorithm.PS256 => SecurityAlgorithms.RsaSsaPssSha256,
                IdentityServerConstants.RsaSigningAlgorithm.PS384 => SecurityAlgorithms.RsaSsaPssSha384,
                IdentityServerConstants.RsaSigningAlgorithm.PS512 => SecurityAlgorithms.RsaSsaPssSha512,
                _ => throw new ArgumentException("Invalid RSA signing algorithm value", nameof(value)),
            };
        }
    }
}
