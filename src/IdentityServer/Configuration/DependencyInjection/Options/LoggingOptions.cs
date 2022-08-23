// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;
using IdentityModel;

namespace Duende.IdentityServer.Configuration;

/// <summary>
/// Options for configuring logging behavior
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// Gets or sets the collection of keys that will be used to redact sensitive values from a backchannel authentication request log.
    /// </summary>
    /// <remarks>Please be aware that initializing this property could expose sensitive information in your logs.</remarks>
    public ICollection<string> BackchannelAuthenticationRequestSensitiveValuesFilter { get; set; } =
        new HashSet<string>
        {
            // TODO: IdentityModel
            OidcConstants.TokenRequest.ClientSecret,
            OidcConstants.TokenRequest.ClientAssertion,
            OidcConstants.AuthorizeRequest.IdTokenHint
        };

    /// <summary>
    /// Gets or sets the collection of keys that will be used to redact sensitive values from a token request log.
    /// </summary>
    /// <remarks>Please be aware that initializing this property could expose sensitive information in your logs.</remarks>
    public ICollection<string> TokenRequestSensitiveValuesFilter { get; set; } = 
        new HashSet<string>
        {
            OidcConstants.TokenRequest.ClientSecret,
            OidcConstants.TokenRequest.Password,
            OidcConstants.TokenRequest.ClientAssertion,
            OidcConstants.TokenRequest.RefreshToken,
            OidcConstants.TokenRequest.DeviceCode
        };

    /// <summary>
    /// Gets or sets the collection of keys that will be used to redact sensitive values from an authorize request log.
    /// </summary>
    /// <remarks>Please be aware that initializing this property could expose sensitive information in your logs.</remarks>
    public ICollection<string> AuthorizeRequestSensitiveValuesFilter { get; set; } = 
        new HashSet<string>
        {
            OidcConstants.AuthorizeRequest.IdTokenHint
        };
}