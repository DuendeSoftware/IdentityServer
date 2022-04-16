using System.Text.Json;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Configuration.EntityFramework.Clients;

public static class ClientEntityExtensions
{
    public static Client ToModel(this ClientEntity clientEntity)
    {
        var client = JsonSerializer.Deserialize<Client>(clientEntity.Json)!;
        return client;
    }

    public static ClientEntity ToEntity(this Client client)
    {
        var clientJson = JsonSerializer.Serialize(client);
        var clientEntity =  new ClientEntity
        {
            ClientId = client.ClientId,
            Json = clientJson
        };

        clientEntity.AllowedCorsOrigins = client.AllowedCorsOrigins.Select(o => new ClientCorsOrigin
        {
            Client = clientEntity,
            ClientId = clientEntity.ClientId,
            Origin = o,
        }).ToArray();

        return clientEntity;
    }
}