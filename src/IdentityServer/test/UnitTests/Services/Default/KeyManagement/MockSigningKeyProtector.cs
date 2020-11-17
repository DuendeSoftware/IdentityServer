

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services.KeyManagement;
using System;

namespace UnitTests.Services.Default.KeyManagement
{
    class MockSigningKeyProtector : ISigningKeyProtector
    {
        public bool ProtectWasCalled { get; set; }

        public SerializedKey Protect(RsaKeyContainer key)
        {
            ProtectWasCalled = true;
            return new SerializedKey
            {
                Id = key.Id,
                SigningAlgorithm = key.SigningAlgorithm,
                IsX509Certificate = key.HasX509Certificate,
                Created = DateTime.UtcNow,
                Data = KeySerializer.Serialize(key),
            };
        }

        public RsaKeyContainer Unprotect(SerializedKey key)
        {
            return KeySerializer.Deserialize<RsaKeyContainer>(key.Data);
        }
    }
}
