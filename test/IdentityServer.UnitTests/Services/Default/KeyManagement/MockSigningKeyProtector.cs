// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services.KeyManagement;
using Microsoft.AspNetCore.DataProtection;
using System;

namespace UnitTests.Services.Default.KeyManagement;

class MockSigningKeyProtector : ISigningKeyProtector
{
    private IDataProtector _dataProtector;
    public bool ProtectWasCalled { get; set; }
    
    public MockSigningKeyProtector()
    {
        var provider = new EphemeralDataProtectionProvider();
        _dataProtector = provider.CreateProtector("test");
    }

    public SerializedKey Protect(KeyContainer key)
    {
        ProtectWasCalled = true;
        return new SerializedKey
        {
            Id = key.Id,
            Algorithm = key.Algorithm,
            IsX509Certificate = key.HasX509Certificate,
            Created = DateTime.UtcNow,
            Data = _dataProtector.Protect(KeySerializer.Serialize(key)),
        };
    }

    public KeyContainer Unprotect(SerializedKey key)
    {
        return KeySerializer.Deserialize<RsaKeyContainer>(_dataProtector.Unprotect(key.Data));
    }
}
