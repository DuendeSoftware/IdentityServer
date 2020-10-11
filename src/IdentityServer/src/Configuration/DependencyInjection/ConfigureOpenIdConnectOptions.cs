// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Linq;
using Duende.IdentityServer.Infrastructure;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Duende.IdentityServer.Configuration
{
    internal class ConfigureOpenIdConnectOptions : IPostConfigureOptions<OpenIdConnectOptions>
    {
        private string[] _schemes;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ConfigureOpenIdConnectOptions(string[] schemes, IHttpContextAccessor httpContextAccessor)
        {
            _schemes = schemes ?? throw new ArgumentNullException(nameof(schemes));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public void PostConfigure(string name, OpenIdConnectOptions options)
        {
            // no schemes means configure them all
            if (_schemes.Length == 0 || _schemes.Contains(name))
            {
                options.StateDataFormat = new DistributedCacheStateDataFormatter(_httpContextAccessor, name);
            }
        }
    }
}