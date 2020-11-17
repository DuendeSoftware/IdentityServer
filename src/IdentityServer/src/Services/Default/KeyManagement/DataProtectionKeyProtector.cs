// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.DataProtection;
using System;

namespace Duende.IdentityServer.Services.KeyManagement
{
    /// <summary>
    /// Implementation of IKeyProtector based on ASP.NET Core's data protection feature.
    /// </summary>
    public class DataProtectionKeyProtector : ISigningKeyProtector
    {
        private readonly KeyManagementOptions _options;
        private readonly IDataProtector _dataProtectionProvider;

        /// <summary>
        /// Constructor for DataProtectionKeyProtector.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="dataProtectionProvider"></param>
        public DataProtectionKeyProtector(KeyManagementOptions options, IDataProtectionProvider dataProtectionProvider)
        {
            _options = options;
            _dataProtectionProvider = dataProtectionProvider.CreateProtector(nameof(DataProtectionKeyProtector));
        }

        /// <inheritdoc/>
        public SerializedKey Protect(KeyContainer key)
        {
            var data = KeySerializer.Serialize(key);
            
            if (_options.DataProtectKeys)
            {
                data = _dataProtectionProvider.Protect(data);
            }
            
            return new SerializedKey
            {
                Created = DateTime.UtcNow,
                Id = key.Id,
                SigningAlgorithm = key.SigningAlgorithm,
                IsX509Certificate = key.HasX509Certificate,
                Data = data,
                DataProtected = _options.DataProtectKeys,
            };
        }

        /// <inheritdoc/>
        public KeyContainer Unprotect(SerializedKey key)
        {
            var data = key.DataProtected ? 
                _dataProtectionProvider.Unprotect(key.Data) : 
                key.Data;

            var item = key.IsX509Certificate ?
                KeySerializer.Deserialize<X509KeyContainer>(data) :
                KeySerializer.Deserialize<RsaKeyContainer>(data);

            return item;
        }
    }
}
