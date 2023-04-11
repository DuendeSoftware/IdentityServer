// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

#nullable enable

using IdentityModel;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Duende.IdentityServer.Validation;

/// <summary>
/// Models a validated request to the authorize endpoint.
/// </summary>
public class ValidatedAuthorizeRequest : ValidatedRequest
{
    /// <summary>
    /// Gets or sets the type of the response.
    /// </summary>
    /// <value>
    /// The type of the response.
    /// </value>
    public string ResponseType { get; set; } = default!;

    /// <summary>
    /// Gets or sets the response mode.
    /// </summary>
    /// <value>
    /// The response mode.
    /// </value>
    public string ResponseMode { get; set; } = default!;

    /// <summary>
    /// Gets or sets the grant type.
    /// </summary>
    /// <value>
    /// The grant type.
    /// </value>
    public string GrantType { get; set; } = default!;

    /// <summary>
    /// Gets or sets the redirect URI.
    /// </summary>
    /// <value>
    /// The redirect URI.
    /// </value>
    public string RedirectUri { get; set; } = default!;

    /// <summary>
    /// Gets or sets the requested scopes.
    /// </summary>
    /// <value>
    /// The requested scopes.
    /// </value>
    // todo: consider replacing with extension method to access Raw collection; would need to be done wholesale for all props.
    public List<string> RequestedScopes { get; set; } = default!;

    // TODO: typo
    /// <summary>
    /// Gets or sets the requested resource indicators.
    /// </summary>
    public IEnumerable<string>? RequestedResourceIndiators { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether consent was shown.
    /// </summary>
    /// <value>
    ///   <c>true</c> if consent was shown; otherwise, <c>false</c>.
    /// </value>
    public bool WasConsentShown { get; set; }

    /// <summary>
    /// Gets the description the user assigned to the device being authorized.
    /// </summary>
    /// <value>
    /// The description.
    /// </value>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the state.
    /// </summary>
    /// <value>
    /// The state.
    /// </value>
    public string? State { get; set; }

    /// <summary>
    /// Gets or sets the UI locales.
    /// </summary>
    /// <value>
    /// The UI locales.
    /// </value>
    public string? UiLocales { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the request was an OpenID Connect request.
    /// </summary>
    /// <value>
    /// <c>true</c> if the request was an OpenID Connect request; otherwise, <c>false</c>.
    /// </value>
    public bool IsOpenIdRequest { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is API resource request.
    /// </summary>
    /// <value>
    /// <c>true</c> if this instance is API resource request; otherwise, <c>false</c>.
    /// </value>
    public bool IsApiResourceRequest { get; set; }

    /// <summary>
    /// Gets or sets the nonce.
    /// </summary>
    /// <value>
    /// The nonce.
    /// </value>
    public string? Nonce { get; set; }

    /// <summary>
    /// Gets or sets the authentication context reference classes.
    /// </summary>
    /// <value>
    /// The authentication context reference classes.
    /// </value>
    public List<string>? AuthenticationContextReferenceClasses { get; set; }

    /// <summary>
    /// Gets or sets the display mode.
    /// </summary>
    /// <value>
    /// The display mode.
    /// </value>
    public string? DisplayMode { get; set; }

    /// <summary>
    /// Gets or sets the collection of prompt modes.
    /// </summary>
    /// <value>
    /// The collection of prompt modes.
    /// </value>
    public IEnumerable<string> PromptModes { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Gets or sets the collection of original prompt modes.
    /// </summary>
    /// <value>
    /// The collection of original prompt modes.
    /// </value>
    internal IEnumerable<string> OriginalPromptModes { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Gets or sets the collection of suppressed prompt modes.
    /// </summary>
    /// <value>
    /// The collection of suppressed prompt modes.
    /// </value>
    internal IEnumerable<string> SuppressedPromptModes { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Gets or sets the maximum age.
    /// </summary>
    /// <value>
    /// The maximum age.
    /// </value>
    public int? MaxAge { get; set; }

    /// <summary>
    /// Gets or sets the login hint.
    /// </summary>
    /// <value>
    /// The login hint.
    /// </value>
    public string? LoginHint { get; set; }

    /// <summary>
    /// Gets or sets the code challenge
    /// </summary>
    /// <value>
    /// The code challenge
    /// </value>
    public string? CodeChallenge { get; set; }

    /// <summary>
    /// Gets or sets the code challenge method
    /// </summary>
    /// <value>
    /// The code challenge method
    /// </value>
    public string? CodeChallengeMethod { get; set; }

    /// <summary>
    /// Gets or sets the validated contents of the request object (if present)
    /// </summary>
    /// <value>
    /// The request object values
    /// </value>
    public IEnumerable<Claim> RequestObjectValues { get; set; } = new List<Claim>();

    /// <summary>
    /// Gets or sets the request object (either passed by value or retrieved by reference)
    /// </summary>
    /// <value>
    /// The request object
    /// </value>
    public string? RequestObject { get; set; }

    /// <summary>
    /// The thumbprint of the associated DPoP proof key, if one was used.
    /// </summary>
    public string? DPoPKeyThumbprint { get; set; }
    
    /// <summary>
    /// Gets a value indicating whether an access token was requested.
    /// </summary>
    /// <value>
    /// <c>true</c> if an access token was requested; otherwise, <c>false</c>.
    /// </value>
    public bool AccessTokenRequested => ResponseType == OidcConstants.ResponseTypes.IdTokenToken ||
                                        ResponseType == OidcConstants.ResponseTypes.Code ||
                                        ResponseType == OidcConstants.ResponseTypes.CodeIdToken ||
                                        ResponseType == OidcConstants.ResponseTypes.CodeToken ||
                                        ResponseType == OidcConstants.ResponseTypes.CodeIdTokenToken;



    /// <summary>
    /// Initializes a new instance of the <see cref="ValidatedAuthorizeRequest"/> class.
    /// </summary>
    public ValidatedAuthorizeRequest()
    {
        RequestedScopes = new List<string>();
        AuthenticationContextReferenceClasses = new List<string>();
    }
}