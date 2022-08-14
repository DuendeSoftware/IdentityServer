using System.Text.Json.Serialization;
using IdentityModel;
using IdentityModel.Jwk;

namespace Duende.IdentityServer.Configuration;

public class DynamicClientRegistrationDocument
{
    /// <summary>
    /// List of redirection URI strings for use in redirect-based flows such as the authorization code and implicit flows.
    /// </summary>
    /// <remarks>
    /// Clients using flows with redirection must register their redirection URI values.
    /// </remarks>
    [JsonPropertyName(OidcConstants.ClientMetadata.RedirectUris)]
    public ICollection<Uri> RedirectUris { get; set; } = new HashSet<Uri>();

    /// <summary>
    /// List of OAuth 2.0 grant type strings that the client can use at the token endpoint.
    /// </summary>
    /// <remarks>
    /// Example: "authorization_code", "client_credentials", "refresh_token".
    /// </remarks>
    [JsonPropertyName(OidcConstants.ClientMetadata.GrantTypes)]
    public ICollection<string> GrantTypes { get; set; } = new HashSet<string>();

    /// <summary>
    /// Human-readable string name of the client to be presented to the end-user during authorization.
    /// </summary>
    [JsonPropertyName(OidcConstants.ClientMetadata.ClientName)]
    public string? ClientName { get; set; }

    /// <summary>
    /// Web page providing information about the client.
    /// </summary>
    [JsonPropertyName(OidcConstants.ClientMetadata.ClientUri)]
    public Uri? ClientUri { get; set; }

    /// <summary>
    /// URL to a JWK Set document which contains the client's public keys.
    /// </summary>
    /// <remarks>
    /// Use of this parameter is preferred over the "jwks" parameter, as it allows for easier key rotation.
    /// The <see cref="JwksUri"/> and <see cref="Jwks"/> parameters MUST NOT both be present in
    /// the same request or response.
    /// </remarks>
    [JsonPropertyName(OidcConstants.ClientMetadata.JwksUri)]
    public Uri? JwksUri { get; set; }

    /// <summary>
    /// JWK Set document which contains the client's public keys.
    /// </summary>
    /// <remarks>
    /// Use of this parameter is preferred over the "jwks" parameter, as it allows for easier key rotation.
    /// The <see cref="JwksUri"/> and <see cref="Jwks"/> parameters MUST NOT both be present in
    /// the same request or response.
    /// </remarks>
    [JsonPropertyName(OidcConstants.ClientMetadata.Jwks)]
    public JsonWebKeySet? Jwks { get; set; }

    /// <summary>
    /// String containing a space-separated list of scope values that the client can use when requesting access tokens.
    /// </summary>
    /// <remarks>
    /// If omitted, an authorization server may register a client with a default set of scopes.
    /// </remarks>
    [JsonPropertyName(OidcConstants.ClientMetadata.Scope)]
    public string? Scope { get; set; }

    /// <summary>
    /// Default maximum authentication age.
    /// </summary>
    [JsonPropertyName(OidcConstants.ClientMetadata.DefaultMaxAge)]
    public int? DefaultMaxAge { get; set; }
    
    /// <summary>
    /// Custom client metadata fields to include in the serialization.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, object> Extensions { get; } = new Dictionary<string, object>(StringComparer.Ordinal);
    
    
    /// output fields
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; }
    
    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }
}