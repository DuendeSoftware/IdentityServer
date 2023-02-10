using System.Text.Json.Serialization;

namespace Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;

public record DynamicClientRegistrationResponse : DynamicClientRegistrationRequest
{
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }

    [JsonPropertyName("client_secret_expires_at")]
    public long? ClientSecretExpiresAt { get; set; }
}