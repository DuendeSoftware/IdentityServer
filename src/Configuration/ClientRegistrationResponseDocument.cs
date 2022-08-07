using System.Text.Json.Serialization;
using IdentityModel;
using IdentityModel.Jwk;

namespace Duende.IdentityServer.Configuration;

public class DynamicClientRegistrationResponseDocument : DynamicClientRegistrationDocument
{
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }
    
    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }
    
    // epoch time
    [JsonPropertyName("client_secret_expires_at")]
    public long? ClientSecretExpiresAt { get; set; }
}