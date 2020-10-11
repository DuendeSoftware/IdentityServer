// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using IdentityModel;

namespace Duende.IdentityServer.Extensions
{
    /// <summary>
    /// Extensions methods for X509Certificate2
    /// </summary>
    public static class X509CertificateExtensions
    {
        /// <summary>
        /// Create the value of a thumbprint-based cnf claim
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static string CreateThumbprintCnf(this X509Certificate2 certificate)
        {
            var hash = certificate.GetCertHash(HashAlgorithmName.SHA256);
                            
            var values = new Dictionary<string, string>
            {
                { "x5t#S256", Base64Url.Encode(hash) }
            };

            return JsonSerializer.Serialize(values);
        }
    }
}