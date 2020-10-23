
using Duende.IdentityServer.Services.KeyManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTests.Services.Default.KeyManagement
{
    class MockSigningKeyStoreCache : ISigningKeyStoreCache
    {
        public List<RsaKeyContainer> Cache { get; set; } = new List<RsaKeyContainer>();

        public bool GetKeysAsyncWasCalled { get; set; }
        public bool StoreKeysAsyncWasCalled { get; set; }
        public TimeSpan StoreKeysAsyncDuration { get; set; }

        public Task<IEnumerable<RsaKeyContainer>> GetKeysAsync()
        {
            GetKeysAsyncWasCalled = true;
            return Task.FromResult(Cache.AsEnumerable());
        }

        public Task StoreKeysAsync(IEnumerable<RsaKeyContainer> keys, TimeSpan duration)
        {
            StoreKeysAsyncWasCalled = true;
            StoreKeysAsyncDuration = duration;

            Cache = keys.ToList();
            return Task.CompletedTask;
        }
    }
}
