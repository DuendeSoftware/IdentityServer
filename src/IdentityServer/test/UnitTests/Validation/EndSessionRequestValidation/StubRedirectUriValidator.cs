// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Validation;

namespace IdentityServer.UnitTests.Validation.EndSessionRequestValidation
{
    public class StubRedirectUriValidator : IRedirectUriValidator
    {
        public bool IsRedirectUriValid { get; set; }
        public bool IsPostLogoutRedirectUriValid { get; set; }

        public Task<bool> IsPostLogoutRedirectUriValidAsync(string requestedUri, Client client)
        {
            return Task.FromResult(IsPostLogoutRedirectUriValid);
        }

        public Task<bool> IsRedirectUriValidAsync(string requestedUri, Client client)
        {
            return Task.FromResult(IsRedirectUriValid);
        }
    }
}
