namespace Duende.IdentityServer.Configuration.WebApi.v1;

public class GetClientResponse : Client
{
    /// <summary>
    /// Unique ID of the client
    /// </summary>
    public string ClientId { get; set; }

    public int Version { get; set; }
}