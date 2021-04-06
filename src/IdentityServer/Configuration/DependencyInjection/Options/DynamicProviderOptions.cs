// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace Duende.IdentityServer.Configuration
{
    /// <summary>
    /// Configures the dynamic external provider feature.
    /// </summary>
    public class DynamicProviderOptions
    {
        Dictionary<string, DynamicProviderType> _providers = new Dictionary<string, DynamicProviderType>();

        /// <summary>
        /// Prefix in the pipeline for callbacks from external providers.
        /// </summary>
        public PathString PathPrefix { get; set; } = "/federation";

        /// <summary>
        /// Scheme used for signin.
        /// </summary>
        public string SignInScheme { get; set; } = IdentityServerConstants.ExternalCookieAuthenticationScheme;

        /// <summary>
        /// Scheme for signout.
        /// </summary>
        public string SignOutScheme { get; set; } = IdentityServerConstants.DefaultCookieAuthenticationScheme;

        /// <summary>
        /// Duration providers are cached from the database.
        /// </summary>
        public TimeSpan ProviderCacheDuration { get; set; } = TimeSpan.FromMinutes(60);

        /// <summary>
        /// Registers a provider confiuration model and authenticaiton handler for the protocol type being used.
        /// </summary>
        public void AddProviderType<THandler, TOptions, TIdentityProvider>(string type)
            where THandler : AuthenticationHandler<TOptions>
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

        internal DynamicProviderType FindProviderType(string type)
        {
            return _providers.ContainsKey(type) ? _providers[type] : null;
        }

        internal class DynamicProviderType
        {
            public Type HandlerType { get; set; }
            public Type OptionsType { get; set; }
            public Type IdentityProviderType { get; set; }
        }
    }
}