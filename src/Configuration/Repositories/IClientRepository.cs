using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Configuration.Repositories;

/// <summary>
/// Represents a repository to load, save and query clients.
/// </summary>
public interface IClientRepository
{
    // TODO Discuss#
    // - update with a version for concurrency control
    // - Models don't have a client id (int). The repository models will something like that.
    
    /// <summary>
    /// Loads a client.
    /// </summary>
    /// <param name="clientId">The Client Id to load.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task<Client?> Read(string clientId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a client.
    /// </summary>
    /// <param name="client">The client to save.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task Add(Client client, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a client.
    /// </summary>
    /// <param name="client">The client to update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task Update(Client client, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a client.
    /// </summary>
    /// <param name="clientId">The client id to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task Delete(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the CorsOriginExists on any client.
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> CorsOriginExists(string origin, CancellationToken cancellationToken);
}