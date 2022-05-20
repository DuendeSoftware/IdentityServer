// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace Duende.IdentityServer.Configuration;

/// <summary>
/// Configures the dynamic external provider feature.
/// </summary>
public class DynamicProviderOptions
{
    Dictionary<string, DynamicProviderType> _providers = new Dictionary<string, DynamicProviderType>();

    /// <summary>
    /// Prefix in the pipeline for callbacks from external providers. Defaults to "/federation".
    /// </summary>
    public PathString PathPrefix { get; set; } = "/federation";

    /// <summary>
    /// Scheme used for signin. Defaults to the constant IdentityServerConstants.ExternalCookieAuthenticationScheme.
    /// </summary>
    public string SignInScheme { get; set; } = IdentityServerConstants.ExternalCookieAuthenticationScheme;

    /// <summary>
    /// Scheme for signout. Defaults to the constant IdentityServerConstants.DefaultCookieAuthenticationScheme.
    /// </summary>
    public string SignOutScheme { get; set; } = IdentityServerConstants.DefaultCookieAuthenticationScheme;

    /// <summary>
    /// Registers a provider configuration model and authentication handler for the protocol type being used.
    /// </summary>
    public void AddProviderType<THandler, TOptions, TIdentityProvider>(string type)
        where THandler : IAuthenticationRequestHandler
        where TOptions : AuthenticationSchemeOptions, new()
        where TIdentityProvider : IdentityProvider
    {
        if (_providers.ContainsKey(type)) throw new Exception($"Type '{type}' already configured.");
            
        _providers.Add(type, new DynamicProviderType
        {
            HandlerType = typeof(THandler), 
            OptionsType = typeof(TOptions),
            IdentityProviderType = typeof(TIdentityProvider),
        });
    }

    /// <summary>
    /// Finds the DynamicProviderType registration by protocol type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public DynamicProviderType FindProviderType(string type)
    {
        return _providers.ContainsKey(type) ? _providers[type] : null;
    }

    /// <summary>
    /// Models a provider type registered with the dynamic providers feature.
    /// </summary>
    public class DynamicProviderType
    {
        /// <summary>
        /// The type of the handler.
        /// </summary>
        public Type HandlerType { get; set; }
        /// <summary>
        /// The type of the options.
        /// </summary>
        public Type OptionsType { get; set; }
        /// <summary>
        /// The identity provider protocol type.
        /// </summary>
        public Type IdentityProviderType { get; set; }
    }
}