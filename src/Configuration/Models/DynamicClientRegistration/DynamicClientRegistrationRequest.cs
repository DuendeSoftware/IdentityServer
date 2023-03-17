// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using IdentityModel;

namespace Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;

/// <summary>
/// Represents a dynamic client registration request. The parameters that are
/// supported include a subset of the parameters defined by IANA
/// (https://www.iana.org/assignments/oauth-parameters/oauth-parameters.xhtml#client-metadata),
/// and custom properties needed by IdentityServer.
/// </summary>
public class DynamicClientRegistrationRequest
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
    /// Valid values are "authorization_code", "client_credentials", "refresh_token".
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
    /// Requested Client Authentication method for the Token Endpoint. The
    /// supported options are client_secret_post, client_secret_basic,
    /// client_secret_jwt, private_key_jwt.
    /// </summary>
    [JsonPropertyName(OidcConstants.ClientMetadata.TokenEndpointAuthenticationMethod)]
    
    public string? TokenEndpointAuthenticationMethod { get; set; }

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
    [JsonPropertyName(OidcConstants.ClientMetadata.Jwks)]
    public KeySet? Jwks { get; set; }

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
    /// <remarks>
    /// This is stored as the UserSsoLifetime property of the client.
    /// </remarks>
    [JsonPropertyName(OidcConstants.ClientMetadata.DefaultMaxAge)]
    public int? DefaultMaxAge { get; set; }

    /// <summary>
    /// A software statement containing client metadata values about the client
    /// software as claims.  This is a string value containing the entire signed
    /// JWT.
    /// </summary>
    /// <remark>
    /// The default configuration endpoints do not use the software statement.
    /// It is included in this model to facilitate extensions to the
    /// configuration system.
    /// </remark>
    [JsonPropertyName(OidcConstants.ClientMetadata.SoftwareStatement)]
    public string? SoftwareStatement { get; set; }

    /// <summary>
    /// Custom client metadata fields to include in the serialization.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, object> Extensions { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);
}