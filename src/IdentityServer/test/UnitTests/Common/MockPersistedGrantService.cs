// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;

namespace IdentityServer.UnitTests.Common
{
    public class MockPersistedGrantService : IPersistedGrantService
    {
        public IEnumerable<Grant> GetAllGrantsResult { get; set; }
        public bool RemoveAllGrantsWasCalled { get; set; }

        public Task<IEnumerable<Grant>> GetAllGrantsAsync(string subjectId)
        {
            return Task.FromResult(GetAllGrantsResult ?? Enumerable.Empty<Grant>());
        }

        public Task RemoveAllGrantsAsync(string subjectId, string clientId, string sessionId = null)
        {
            RemoveAllGrantsWasCalled = true;
            return Task.CompletedTask;
        }
    }
}
