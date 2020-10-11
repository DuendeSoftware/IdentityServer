// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Configuration;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Duende.IdentityServer.Extensions
{
    /// <summary>
    /// Extensions for Key Management
    /// </summary>
    public static class KeyManagementExtensionsExtensions
    {
        public static RsaSecurityKey CreateRsaSecurityKey(this KeyManagementOptions options)
        {
            var rsa = RSA.Create();
            RsaSecurityKey key;

            if (rsa is RSACryptoServiceProvider)
            {
                rsa.Dispose();
                var cng = new RSACng(options.KeySize);

                var parameters = cng.ExportParameters(includePrivateParameters: true);
                key = new RsaSecurityKey(parameters);
            }
            else
            {
                rsa.KeySize = options.KeySize;
                key = new RsaSecurityKey(rsa);
            }

            // KeyIdSize is in bits, so convert to bytes
            var size = options.KeyIdSize / 8;
            key.KeyId = CryptoRandom.CreateUniqueId(size, CryptoRandom.OutputFormat.Hex);

            return key;
        }

        public static bool IsRetired(this KeyManagementOptions options, TimeSpan diff)
        {
            return (diff >= options.KeyRetirement);
        }

        public static bool IsExpired(this KeyManagementOptions options, TimeSpan diff)
        {
            return (diff >= options.KeyExpiration);
        }

        public static bool IsWithinInitializationDuration(this KeyManagementOptions options, TimeSpan diff)
        {
            return (diff <= options.InitializationDuration);
        }

        public static TimeSpan GetAge(this ISystemClock clock, DateTime date)
        {
            var now = clock.UtcNow.DateTime;
            if (date > now) now = date;
            return now.Subtract(date);
        }
   }
}