namespace Duende.IdentityServer.Configuration.EntityFramework.Clients;

public class ClientEntity
{
    public string ClientId { get; set; }

    public ICollection<ClientCorsOrigin> AllowedCorsOrigins { get; set; }

    public string Json { get; set; }
}