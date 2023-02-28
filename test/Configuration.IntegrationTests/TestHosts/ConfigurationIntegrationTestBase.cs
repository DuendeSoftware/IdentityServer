// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

// using Duende.IdentityServer.Models;
// using Duende.IdentityServer.Services;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IntegrationTests.TestHosts;

public class ConfigurationIntegrationTestBase
{
    protected readonly IdentityServerHost IdentityServerHost;
    protected readonly ConfigurationHost ConfigurationHost;

    public ConfigurationIntegrationTestBase()
    {
        var dbRoot = new InMemoryDatabaseRoot();
        IdentityServerHost = new IdentityServerHost(dbRoot);
        IdentityServerHost.InitializeAsync().Wait();

        ConfigurationHost = new ConfigurationHost(
            authority: IdentityServerHost.Server!.BaseAddress.ToString(),
            identityServerHttpClient: IdentityServerHost.HttpClient!,
            dbRoot);
        ConfigurationHost.InitializeAsync().Wait();
    }
}
