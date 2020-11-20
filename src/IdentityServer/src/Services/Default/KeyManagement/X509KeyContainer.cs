// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;

namespace Duende.IdentityServer.Services.KeyManagement
{
    /// <summary>
    /// Container class for X509 keys.
    /// </summary>
    public class X509KeyContainer : KeyContainer
    {
        private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";

        /// <summary>
        /// Constructor for X509KeyContainer.
        /// </summary>
        public X509KeyContainer()
        {
            HasX509Certificate = true;
        }

        /// <summary>
        /// Constructor for X509KeyContainer.
        /// </summary>
        public X509KeyContainer(RsaSecurityKey key, string algorithm, DateTime created, TimeSpan certAge, string issuer = "OP")
            : base(key.KeyId, algorithm, created)
        {
            HasX509Certificate = true;
            
            var distinguishedName = new X500DistinguishedName($"CN={issuer}");

            var request = new CertificateRequest(
              distinguishedName, key.Rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));

            request.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection { new Oid(ServerAuthenticationOid) }, false));

            _cert = request.CreateSelfSigned(
                new DateTimeOffset(created, TimeSpan.Zero),
                new DateTimeOffset(created.Add(certAge), TimeSpan.Zero));

            CertificateRawData = Convert.ToBase64String(_cert.Export(X509ContentType.Pfx));
        }

        /// <summary>
        /// Constructor for X509KeyContainer.
        /// </summary>
        public X509KeyContainer(ECDsaSecurityKey key, string algorithm, DateTime created, TimeSpan certAge, string issuer = "OP")
            : base(key.KeyId, algorithm, created)
        {
            HasX509Certificate = true;

            var distinguishedName = new X500DistinguishedName($"CN={issuer}");

            //var ec = ECDsa.Create(key.ECDsa.par)
            var request = new CertificateRequest(distinguishedName, key.ECDsa, HashAlgorithmName.SHA256);

            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));

            request.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection { new Oid(ServerAuthenticationOid) }, false));

            _cert = request.CreateSelfSigned(
                new DateTimeOffset(created, TimeSpan.Zero),
                new DateTimeOffset(created.Add(certAge), TimeSpan.Zero));

            CertificateRawData = Convert.ToBase64String(_cert.Export(X509ContentType.Pfx));
        }

        private X509Certificate2 _cert;

        /// <summary>
        /// The X509 certificate data.
        /// </summary>
        public string CertificateRawData { get; set; }

        /// <inheritdoc />
        public override AsymmetricSecurityKey ToSecurityKey()
        {
            if (_cert == null)
            {
                _cert = new X509Certificate2(Convert.FromBase64String(CertificateRawData));
            }

            var key = new X509SecurityKey(_cert, Id);
            return key;
        }
    }
}
