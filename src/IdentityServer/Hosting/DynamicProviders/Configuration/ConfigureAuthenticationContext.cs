// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Duende.IdentityServer.Hosting.DynamicProviders;

/// <summary>
/// Context for configuring an authentication handler from a dynamic identity provider.
/// </summary>
/// <typeparam name="TAuthenticationOptions"></typeparam>
/// <typeparam name="TIdentityProvider"></typeparam>
public class ConfigureAuthenticationContext<TAuthenticationOptions, TIdentityProvider>
    where TAuthenticationOptions : RemoteAuthenticationOptions
    where TIdentityProvider : IdentityProvider
{
    /// <summary>
    /// The authentication options.
    /// </summary>
    public TAuthenticationOptions AuthenticationOptions { get; set; }

    /// <summary>
    /// The identity provider.
    /// </summary>
    public TIdentityProvider IdentityProvider { get; set; }

    /// <summary>
    /// The dynamic identity provider options.
    /// </summary>
    public DynamicProviderOptions DynamicProviderOptions { get; set; }

    /// <summary>
    /// The path prefix for callback paths the authentication handler is to use.
    /// </summary>
    public PathString PathPrefix { get; set; }
}