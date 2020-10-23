// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using System;

namespace Duende.IdentityServer.Extensions
{
    /// <summary>
    /// Extensions for Key Management
    /// </summary>
    public static class KeyManagementExtensionsExtensions
    {
        internal static RsaSecurityKey CreateRsaSecurityKey(this KeyManagementOptions options)
        {
            return CryptoHelper.CreateRsaSecurityKey(options.KeySize);
        }

        internal static bool IsRetired(this KeyManagementOptions options, TimeSpan diff)
        {
            return (diff >= options.KeyRetirement);
        }

        internal static bool IsExpired(this KeyManagementOptions options, TimeSpan diff)
        {
            return (diff >= options.KeyExpiration);
        }

        internal static bool IsWithinInitializationDuration(this KeyManagementOptions options, TimeSpan diff)
        {
            return (diff <= options.InitializationDuration);
        }

        internal static TimeSpan GetAge(this ISystemClock clock, DateTime date)
        {
            var now = clock.UtcNow.DateTime;
            if (date > now) now = date;
            return now.Subtract(date);
        }
   }
}