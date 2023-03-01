using System.Text.Json.Serialization;

namespace Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;

public class DynamicClientRegistrationResponse : DynamicClientRegistrationRequest, IDynamicClientRegistrationResponse
{
    public DynamicClientRegistrationResponse()
    {
    }

    public DynamicClientRegistrationResponse(DynamicClientRegistrationRequest request)
    {
        RedirectUris = request.RedirectUris;
        GrantTypes = request.GrantTypes;
        ClientName = request.ClientName;
        ClientUri = request.ClientUri;
        JwksUri = request.JwksUri;
        Jwks = request.Jwks;
        Scope = request.Scope;
        DefaultMaxAge = request.DefaultMaxAge;
        Extensions = request.Extensions;
    }

    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }

    [JsonPropertyName("client_secret_expires_at")]
    public long? ClientSecretExpiresAt { get; set; }
}
