// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authentication;
using System;

namespace Duende.IdentityServer.Hosting.DynamicProviders
{
    class DynamicAuthenticationScheme : AuthenticationScheme
    {
        public DynamicAuthenticationScheme(IdentityProvider idp, Type handlerType)
            : base(idp.Scheme, idp.DisplayName, handlerType)
        {
            IdentityProvider = idp;
        }

        public IdentityProvider IdentityProvider { get; set; }
    }
}
