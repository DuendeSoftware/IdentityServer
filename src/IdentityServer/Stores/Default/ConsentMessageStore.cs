// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Stores
{
    internal class ConsentMessageStore : IConsentMessageStore
    {
        protected readonly MessageCookie<ConsentResponse> Cookie;

        public ConsentMessageStore(MessageCookie<ConsentResponse> cookie)
        {
            Cookie = cookie;
        }

        public virtual Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            Cookie.Clear(id);
            return Task.CompletedTask;
        }

        public virtual Task<Message<ConsentResponse>> ReadAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Cookie.Read(id));
        }

        public virtual Task WriteAsync(string id, Message<ConsentResponse> message, CancellationToken cancellationToken = default)
        {
            Cookie.Write(id, message);
            return Task.CompletedTask;
        }
    }
}
