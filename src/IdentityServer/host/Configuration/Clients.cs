// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Models;
using System.Collections.Generic;

namespace IdentityServerHost.Configuration
{
    public static class Clients
    {
        public static IEnumerable<Client> Get()
        {
            var clients = new List<Client>();

            clients.AddRange(ClientsConsole.Get());
            clients.AddRange(ClientsWeb.Get());

            return clients;
        }
    }
}