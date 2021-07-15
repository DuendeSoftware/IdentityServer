
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTests.Services.Default.KeyManagement
{
    class MockSigningKeyStore : ISigningKeyStore
    {
        public List<SerializedKey> Keys { get; set; } = new List<SerializedKey>();
        public bool LoadKeysAsyncWasCalled { get; set; }
        public bool DeleteWasCalled { get; set; }

        public Task DeleteKeyAsync(string id, CancellationToken cancellationToken)
        {
            DeleteWasCalled = true;
            if (Keys != null)
            {
                Keys.Remove(Keys.FirstOrDefault(x => x.Id == id));
            }
            return Task.CompletedTask;
        }

        public Task<IEnumerable<SerializedKey>> LoadKeysAsync(CancellationToken cancellationToken)
        {
            LoadKeysAsyncWasCalled = true;
            return Task.FromResult<IEnumerable<SerializedKey>>(Keys);
        }

        public Task StoreKeyAsync(SerializedKey key, CancellationToken cancellationToken)
        {
            if (Keys == null)
            {
                Keys = new List<SerializedKey>();
            }

            Keys.Add(key);
            return Task.CompletedTask;
        }
    }
}
