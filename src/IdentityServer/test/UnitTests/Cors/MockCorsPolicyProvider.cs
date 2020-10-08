// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace IdentityServer.UnitTests.Cors
{
    public class MockCorsPolicyProvider : ICorsPolicyProvider
    {
        public bool WasCalled { get; set; }
        public CorsPolicy Response { get; set; }

        public Task<CorsPolicy> GetPolicyAsync(HttpContext context, string policyName)
        {
            WasCalled = true;
            return Task.FromResult(Response);
        }
    }
}
