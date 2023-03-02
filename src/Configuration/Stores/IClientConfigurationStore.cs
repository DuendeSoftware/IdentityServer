// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Configuration;

public interface IClientConfigurationStore
{
    Task AddAsync(Client client);
}