// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using Duende.IdentityServer.Services;

namespace UnitTests.Validation.Setup
{
    internal class TestIssuerNameService : IIssuerNameService
    {
        private string _value;

        public TestIssuerNameService(string value)
        {
            _value = value ?? "https://identityserver";         
        }
        public string GetCurrent()
        {
            return _value;
        }
    }
}