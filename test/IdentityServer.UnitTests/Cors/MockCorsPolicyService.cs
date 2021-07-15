// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.Services;

namespace UnitTests.Cors
{
    public class MockCorsPolicyService : ICorsPolicyService
    {
        public bool WasCalled { get; set; }
        public bool Response { get; set; }

        public Task<bool> IsOriginAllowedAsync(string origin, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(Response);
        }
    }
}
