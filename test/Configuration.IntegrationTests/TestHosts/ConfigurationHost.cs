// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using IntegrationTests.TestFramework;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Configuration.EntityFramework;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Duende.IdentityServer.Services;

namespace IntegrationTests.TestHosts;

public class ConfigurationHost : GenericHost
{
    private readonly string _authority;

    public ConfigurationHost(
        string authority,
        InMemoryDatabaseRoot databaseRoot,
        string baseAddress = "https://configuration") 
            : base(baseAddress)
    {
        OnConfigureServices += (services) => ConfigureServices(services, databaseRoot);
        OnConfigure += Configure;
        _authority = authority;
    }

    private void ConfigureServices(IServiceCollection services, InMemoryDatabaseRoot databaseRoot)
    {
        services.AddRouting();
        services.AddAuthorization();

        services.AddLogging(logging => {
            logging.AddFilter("Duende", LogLevel.Debug);
        });

        services.AddSingleton<ICancellationTokenProvider, MockCancellationTokenProvider>();

        services.AddIdentityServerConfiguration(opt =>
            opt.Authority = _authority
        );
        services.AddClientConfigurationStore();

        services.AddDbContext<ConfigurationDbContext>(opt =>
        opt.UseInMemoryDatabase("configurationDb", databaseRoot));
    }

    private void Configure(WebApplication app)
    {
        app.UseRouting();

        app.UseAuthorization();


        app.MapDynamicClientRegistration("/connect/dcr")
            .AllowAnonymous();
    }
}
