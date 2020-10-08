// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Validation;

namespace IdentityServer.IntegrationTests.Clients.Setup
{
    public class ExtensionGrantValidator : IExtensionGrantValidator
    {
        public Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            var credential = context.Request.Raw.Get("custom_credential");
            var extraClaim = context.Request.Raw.Get("extra_claim");

            if (credential != null)
            {
                if (extraClaim != null)
                {
                    context.Result = new GrantValidationResult(
                        subject: "818727",
                        claims: new[] { new Claim("extra_claim", extraClaim) },
                        authenticationMethod: GrantType);
                }
                else
                {
                    context.Result = new GrantValidationResult(subject: "818727", authenticationMethod: GrantType);
                }
            }
            else
            {
                // custom error message
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "invalid_custom_credential");
            }

            return Task.CompletedTask;
        }

        public string GrantType =>  "custom";
    }
}