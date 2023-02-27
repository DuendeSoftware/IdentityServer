using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Configuration;

public class ClientConfigurationStore : IClientConfigurationStore
{
    /// <summary>
    /// The DbContext.
    /// </summary>
    protected readonly ConfigurationDbContext DbContext;

    /// <summary>
    /// The CancellationToken provider.
    /// </summary>
    protected readonly ICancellationTokenProvider CancellationTokenProvider;

    /// <summary>
    /// The logger.
    /// </summary>
    protected readonly ILogger<ClientConfigurationStore> Logger;

    public ClientConfigurationStore(
        ConfigurationDbContext dbContext, 
        ICancellationTokenProvider cancellationTokenProvider,
        ILogger<ClientConfigurationStore> logger)
    {
        DbContext = dbContext;
        CancellationTokenProvider = cancellationTokenProvider;
        Logger = logger;
    }

    public async Task AddAsync(Client client)
    {
        // TODO - nicer log message
        Logger.LogDebug("Adding a client"); 
        DbContext.Clients.Add(ClientMappers.ToEntity(client));
        await DbContext.SaveChangesAsync(CancellationTokenProvider.CancellationToken);
    }
}