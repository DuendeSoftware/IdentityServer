// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Text.Json.Serialization;

namespace Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;

/// <summary>
/// Represents the response to a successful dynamic client registration request.
/// </summary>
public class DynamicClientRegistrationResponse : DynamicClientRegistrationRequest, IDynamicClientRegistrationResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicClientRegistrationResponse"/> class.
    /// </summary>
    public DynamicClientRegistrationResponse()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicClientRegistrationResponse"/> class copying properties from the specified request.
    /// </summary>
    /// <param name="request">The request used to initialize the response.</param>
    public DynamicClientRegistrationResponse(DynamicClientRegistrationRequest request)
    {
        // TODO - verify that all these parameters should be echoed back in the
        // response
        RedirectUris = request.RedirectUris;
        GrantTypes = request.GrantTypes;
        ClientName = request.ClientName;
        ClientUri = request.ClientUri;
        JwksUri = request.JwksUri;
        Jwks = request.Jwks;
        Scope = request.Scope;
        DefaultMaxAge = request.DefaultMaxAge;
        Extensions = request.Extensions;
        SoftwareStatement = request.SoftwareStatement;
    }

    /// <summary>
    /// Gets or sets the client ID.
    /// </summary>
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client secret.
    /// </summary>
    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the expiration time of the client secret.
    /// </summary>
    [JsonPropertyName("client_secret_expires_at")]
    public long? ClientSecretExpiresAt { get; set; }
}