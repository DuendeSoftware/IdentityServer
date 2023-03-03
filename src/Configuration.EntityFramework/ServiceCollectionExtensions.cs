// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace Duende.IdentityServer.Configuration.EntityFramework;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClientConfigurationStore(this IdentityServerConfigurationBuilder builder)
    {
        return builder.Services.AddTransient<IClientConfigurationStore, ClientConfigurationStore>();
    }
}