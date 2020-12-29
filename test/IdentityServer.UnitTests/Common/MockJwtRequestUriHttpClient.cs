// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;

namespace UnitTests.Common
{
    public class MockJwtRequestUriHttpClient : IJwtRequestUriHttpClient
    {
        public string Jwt { get; set; }

        public Task<string> GetJwtAsync(string url, Client client)
        {
            return Task.FromResult(Jwt);
        }
    }
}
