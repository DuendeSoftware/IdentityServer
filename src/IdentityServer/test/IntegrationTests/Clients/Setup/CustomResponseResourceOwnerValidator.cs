// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Validation;

namespace IdentityServer.IntegrationTests.Clients.Setup
{
    public class CustomResponseResourceOwnerValidator : IResourceOwnerPasswordValidator
    {
        public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            var response = new Dictionary<string, object>
            {
                { "string_value", "some_string" },
                { "int_value", 42 },
                { "dto",  CustomResponseDto.Create }
            };

            if (context.UserName == context.Password)
            {
                context.Result = new GrantValidationResult(context.UserName, "password", customResponse: response);
            }
            else
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "invalid_credential", response);
            }

            return Task.CompletedTask;
        }
    }
}