// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Duende.IdentityServer.Hosting.FederatedSignOut
{
    internal class AuthenticationRequestSignOutHandlerWrapper : AuthenticationRequestHandlerWrapper, IAuthenticationSignOutHandler
    {
        private readonly IAuthenticationSignOutHandler _inner;

        public AuthenticationRequestSignOutHandlerWrapper(IAuthenticationSignOutHandler inner, IHttpContextAccessor httpContextAccessor)
            : base((IAuthenticationRequestHandler)inner, httpContextAccessor)
        {
            _inner = inner;
        }

        public Task SignOutAsync(AuthenticationProperties properties)
        {
            return _inner.SignOutAsync(properties);
        }
    }
}
