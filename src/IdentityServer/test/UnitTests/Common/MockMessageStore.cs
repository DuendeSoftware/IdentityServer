// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace IdentityServer.UnitTests.Common
{
    public class MockMessageStore<TModel> : IMessageStore<TModel>
    {
        public Dictionary<string, Message<TModel>> Messages { get; set; } = new Dictionary<string, Message<TModel>>();

        public Task<Message<TModel>> ReadAsync(string id)
        {
            Message<TModel> val = null;
            if (id != null)
            {
                Messages.TryGetValue(id, out val);
            }
            return Task.FromResult(val);
        }

        public Task<string> WriteAsync(Message<TModel> message)
        {
            var id = Guid.NewGuid().ToString();
            Messages[id] = message;
            return Task.FromResult(id);
        }
    }
}
