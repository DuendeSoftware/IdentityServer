// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace IntegrationTests.Common
{
    internal static class TestCert
    {
        public static X509Certificate2 Load()
        {
            var cert = Path.Combine(System.AppContext.BaseDirectory, "identityserver_testing.pfx");
            return new X509Certificate2(cert, "password");
        }
    }
}