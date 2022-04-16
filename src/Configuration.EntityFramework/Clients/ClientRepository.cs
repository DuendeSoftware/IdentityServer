using Duende.IdentityServer.Configuration.EntityFramework.DbContexts;
using Duende.IdentityServer.Configuration.Repositories;
using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Configuration.EntityFramework.Clients;

public class ClientRepository : Repository, IClientRepository
{
    public ClientRepository(ConfigurationDbContext context, ILogger<ClientRepository> logger)
        :base(context, logger)
    { }

    /// <inheritdoc/>
    public virtual async Task<Client?> Read(string clientId, CancellationToken cancellationToken = default)
    {
        using var activity = Tracing.ConfigurationActivitySource.StartActivity();
        var query = Context.Clients!.Where(x => x.ClientId == clientId);

        var clientEntities = await query.ToArrayAsync(cancellationToken);
        var clientEntity = clientEntities.SingleOrDefault(x => x.ClientId == clientId);
        if (clientEntity == null)
        {
            Logger.LogDebug("{clientId} found in database: {clientIdFound}", clientId, false);
            return null;
        }
        Logger.LogDebug("{clientId} found in database: {clientIdFound}", clientId, true);

        return clientEntity.ToModel();
    }

    /// <inheritdoc/>
    public virtual async Task Add(Client client, CancellationToken cancellationToken = default)
    {
        var clientEntity = client.ToEntity();
        Context.Clients!.Add(clientEntity);
        await Context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task Update(Client client, CancellationToken cancellationToken = default)
    {
        var clientEntity = client.ToEntity();
        Context.Clients!.Update(clientEntity);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public Task Delete(string clientId, CancellationToken cancellationToken = default)
    {
        //TODO: can't delete by id?
        //Context.Clients!.Remove();
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public virtual async Task<bool> CorsOriginExists(string origin, CancellationToken cancellationToken = default)
    {
        origin = origin.ToLowerInvariant();

        var query1 = from o in Context.ClientCorsOrigins
            select o;
        var clientCorsOrigins = await query1.ToListAsync(cancellationToken);


        var query = from o in Context.ClientCorsOrigins
            where o.Origin == origin
            select o;

        var isAllowed = await query.AnyAsync(cancellationToken);

        Logger.LogDebug("Origin {origin} is allowed: {originAllowed}", origin, isAllowed);

        return isAllowed;
    }
}