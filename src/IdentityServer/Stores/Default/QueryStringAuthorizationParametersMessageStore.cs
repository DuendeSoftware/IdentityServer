// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Extensions;
using System.Threading;

namespace Duende.IdentityServer.Stores
{
    // internal just for testing
    internal class QueryStringAuthorizationParametersMessageStore : IAuthorizationParametersMessageStore
    {
        public Task<string> WriteAsync(Message<IDictionary<string, string[]>> message, CancellationToken cancellationToken = default)
        {
            var queryString = message.Data.FromFullDictionary().ToQueryString();
            return Task.FromResult(queryString);
        }

        public Task<Message<IDictionary<string, string[]>>> ReadAsync(string id, CancellationToken cancellationToken = default)
        {
            var values = id.ReadQueryStringAsNameValueCollection();
            var msg = new Message<IDictionary<string, string[]>>(values.ToFullDictionary());
            return Task.FromResult(msg);
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
