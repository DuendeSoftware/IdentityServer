// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using Duende.IdentityServer.Models;
using IdentityModel;

namespace Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;

/// <summary>
/// Represents the response to a successful dynamic client registration client.
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
    /// Initializes a new instance of the <see
    /// cref="DynamicClientRegistrationResponse"/> class copying properties from
    /// the specified request and client. This tries to copy from the client's
    /// properties, and only uses the request if it must. Doing so means that the 
    /// response will better reflect the actually created client record.
    /// </summary>
    /// <param name="request">The request used to initialize the
    /// response.</param>
    /// <param name="client">The client used to initialize the response.</param>
    public DynamicClientRegistrationResponse(DynamicClientRegistrationRequest request, Client client)
    {
        // Software Statement
        SoftwareStatement = request.SoftwareStatement;
        SoftwareId = request.SoftwareId;
        SoftwareVersion = request.SoftwareVersion;

        // Grant Types
        GrantTypes = client.AllowedGrantTypes;
        if(client.AllowOfflineAccess)
        {
            GrantTypes.Add(OidcConstants.GrantTypes.RefreshToken);
        }
        AuthorizationCodeLifetime = client.AuthorizationCodeLifetime;
        RefreshTokenExpiration = client.RefreshTokenExpiration.ToString();
        RefreshTokenUsage = client.RefreshTokenUsage.ToString();
        AbsoluteRefreshTokenLifetime = client.AbsoluteRefreshTokenLifetime;
        SlidingRefreshTokenLifetime = client.SlidingRefreshTokenLifetime;
        UpdateAccessTokenClaimsOnRefresh = client.UpdateAccessTokenClaimsOnRefresh;

        // Redirect Uris
        RedirectUris = client.RedirectUris.Select(s => new Uri(s)).ToList();

        // Scopes
        Scope = string.Join(' ', client.AllowedScopes);

        // Secrets
        JwksUri = request.JwksUri;
        Jwks = request.Jwks;
        TokenEndpointAuthenticationMethod = request.TokenEndpointAuthenticationMethod;
        RequireSignedRequestObject = client.RequireRequestObject;

        // Client Name
        ClientName = client.ClientName;

        // Logout Parameters
        PostLogoutRedirectUris = client.PostLogoutRedirectUris.Select(s => new Uri(s)).ToList();
        FrontChannelLogoutUri = ToUri(client.FrontChannelLogoutUri);
        FrontChannelLogoutSessionRequired = client.FrontChannelLogoutSessionRequired;
        BackChannelLogoutUri = ToUri(client.BackChannelLogoutUri);
        BackChannelLogoutSessionRequired = client.BackChannelLogoutSessionRequired;

        // Max Age
        DefaultMaxAge = client.UserSsoLifetime;

        // User Interface
        LogoUri = ToUri(client.LogoUri);
        InitiateLoginUri = ToUri(client.InitiateLoginUri);
        EnableLocalLogin = client.EnableLocalLogin;
        IdentityProviderRestrictions = client.IdentityProviderRestrictions.ToHashSet();
        RequireConsent = client.RequireConsent;
        ClientUri = ToUri(client.ClientUri);
        AllowRememberConsent = client.AllowRememberConsent;
        ConsentLifetime = client.ConsentLifetime;

        // Public Clients
        AllowedCorsOrigins = client.AllowedCorsOrigins.ToHashSet();
        RequireClientSecret = client.RequireClientSecret;

        // Access Token
        AccessTokenType = client.AccessTokenType.ToString();
        AccessTokenLifetime = client.AccessTokenLifetime;

        // ID Token
        IdentityTokenLifetime = client.IdentityTokenLifetime;
        AllowedIdentityTokenSigningAlgorithms = client.AllowedIdentityTokenSigningAlgorithms;

        // Server Side Sessions
        CoordinateLifetimeWithUserSession = client.CoordinateLifetimeWithUserSession;

        // Extensions
        Extensions = request.Extensions;
    }

    private static Uri? ToUri(string? s) =>
        s != null ? new Uri(s) : null;

    /// <summary>
    /// Gets or sets the client ID.
    /// </summary>
    [JsonPropertyName(OidcConstants.RegistrationResponse.ClientId)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client secret.
    /// </summary>
    [JsonPropertyName(OidcConstants.RegistrationResponse.ClientSecret)]
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the expiration time of the client secret.
    /// </summary>
    [JsonPropertyName(OidcConstants.RegistrationResponse.ClientSecretExpiresAt)]
    public long? ClientSecretExpiresAt { get; set; }

    /// <summary>
    /// List of the OAuth 2.0 response type strings that the client can use at the authorization endpoint.
    /// </summary>
    /// <remarks>
    /// Example: "code" or "token".
    /// </remarks>
    [JsonPropertyName(OidcConstants.ClientMetadata.ResponseTypes)]
    public ICollection<string> ResponseTypes { get; set; } = new HashSet<string>();
}