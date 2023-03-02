// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Text.Json.Serialization;

namespace Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;

public class DynamicClientRegistrationErrorResponse : IDynamicClientRegistrationResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("error_description")]
    public string ErrorDescription { get; set; } = string.Empty;
}