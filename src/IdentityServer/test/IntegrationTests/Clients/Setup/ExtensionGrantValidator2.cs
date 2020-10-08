// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;
using IdentityServer4.Validation;

namespace IdentityServer.IntegrationTests.Clients.Setup
{
    public class ExtensionGrantValidator2 : IExtensionGrantValidator
    {
        public Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            var credential = context.Request.Raw.Get("custom_credential");

            if (credential != null)
            {
                // valid credential
                context.Result = new GrantValidationResult("818727", "custom");
            }
            else
            {
                // custom error message
                context.Result = new GrantValidationResult(IdentityServer4.Models.TokenRequestErrors.InvalidGrant, "invalid custom credential");
            }

            return Task.CompletedTask;
        }

        public string GrantType => "custom2";
    }
}