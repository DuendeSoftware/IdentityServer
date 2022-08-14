using System.Text.Json.Serialization;

namespace Duende.IdentityServer.Configuration;

public class DynamicClientRegistrationErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; }
    
    [JsonPropertyName("error_description")]
    public string ErrorDescription { get; set; }
}