using System.Text.Json.Serialization;

namespace Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;

public class DynamicClientRegistrationErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("error_description")]
    public string ErrorDescription { get; set; } = string.Empty;
}