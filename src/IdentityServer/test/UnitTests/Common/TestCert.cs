// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;

namespace IdentityServer.UnitTests.Common
{
    internal static class TestCert
    {
        public static X509Certificate2 Load()
        {
            var cert = Path.Combine(System.AppContext.BaseDirectory, "identityserver_testing.pfx");
            return new X509Certificate2(cert, "password");
        }

        public static SigningCredentials LoadSigningCredentials()
        {
            var cert = Load();
            return new SigningCredentials(new X509SecurityKey(cert), "RS256");
        }
    }
}