namespace Duende.IdentityServer.Configuration.EntityFramework.Clients;

public class ClientCorsOrigin
{
    public int Id { get; set; }
 
    public string Origin { get; set; }

    public string ClientId { get; set; }

    public ClientEntity Client { get; set; }
}