using System.Text.Json.Serialization;
using IdentityModel;
using IdentityModel.Jwk;

namespace Duende.IdentityServer.Configuration;

public class DynamicClientRegistrationResponse : DynamicClientRegistrationRequest
{
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }
    
    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }
    
    [JsonPropertyName("client_secret_expires_at")]
    public long ClientSecretExpiresAt { get; set; } = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
}