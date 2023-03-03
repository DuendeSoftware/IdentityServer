// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Text.Json.Serialization;

namespace Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;

/// <summary>
/// Represents an error response to a dynamic client registration request.
/// </summary>
public class DynamicClientRegistrationErrorResponse : IDynamicClientRegistrationResponse
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error description.
    /// </summary>
    [JsonPropertyName("error_description")]
    public string ErrorDescription { get; set; } = string.Empty;
}