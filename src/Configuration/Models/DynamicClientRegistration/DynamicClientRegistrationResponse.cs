// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using IdentityModel;

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
    /// Initializes a new instance of the <see
    /// cref="DynamicClientRegistrationResponse"/> class copying properties from
    /// the specified request.
    /// </summary>
    /// <param name="request">The request used to initialize the
    /// response.</param>
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
        
        SoftwareStatement = request.SoftwareStatement;
        SoftwareId = request.SoftwareId;
        SoftwareVersion = request.SoftwareVersion;
        
        PostLogoutRedirectUris = request.PostLogoutRedirectUris;
        FrontChannelLogoutUri = request.FrontChannelLogoutUri;
        FrontChannelLogoutSessionRequired = request.FrontChannelLogoutSessionRequired;
        BackChannelLogoutUri = request.BackChannelLogoutUri;
        BackchannelLogoutSessionRequired = request.BackchannelLogoutSessionRequired;
        
        LogoUri = request.LogoUri;
        InitiateLoginUri = request.InitiateLoginUri;
        RequireSignedRequestObject = request.RequireSignedRequestObject;
        TokenEndpointAuthenticationMethod = request.TokenEndpointAuthenticationMethod;
        RefreshTokenExpiration = request.RefreshTokenExpiration;
        AbsoluteRefreshTokenLifetime = request.AbsoluteRefreshTokenLifetime;
        SlidingRefreshTokenLifetime = request.SlidingRefreshTokenLifetime;
        AuthorizationCodeLifetime = request.AuthorizationCodeLifetime;
        RefreshTokenUsage = request.RefreshTokenUsage;
        UpdateAccessTokenClaimsOnRefresh = request.UpdateAccessTokenClaimsOnRefresh;
        
        AllowAccessTokensViaBrowser = request.AllowAccessTokensViaBrowser;
        AllowedCorsOrigins = request.AllowedCorsOrigins;
        RequireClientSecret = request.RequireClientSecret;

        EnableLocalLogin = request.EnableLocalLogin;
        IdentityProviderRestrictions = request.IdentityProviderRestrictions;
        RequireConsent = request.RequireConsent;
        AllowRememberConsent = request.AllowRememberConsent;
        ConsentLifetime = request.ConsentLifetime;

        AccessTokenType = request.AccessTokenType;
        AccessTokenLifetime = request.AccessTokenLifetime;

        IdentityTokenLifetime = request.IdentityTokenLifetime;
        AllowedIdentityTokenSigningAlgorithms = request.AllowedIdentityTokenSigningAlgorithms;

        CoordinateLifetimeWithUserSession = request.CoordinateLifetimeWithUserSession;
    }

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