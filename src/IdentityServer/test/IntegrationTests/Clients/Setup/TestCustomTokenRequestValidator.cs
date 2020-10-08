// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Validation;

namespace IdentityServer.IntegrationTests.Clients.Setup
{
    public class TestCustomTokenRequestValidator : ICustomTokenRequestValidator
    {
        public Task ValidateAsync(CustomTokenRequestValidationContext context)
        {
            context.Result.CustomResponse = new Dictionary<string, object>
            {
                {"custom", "custom" }
            };

            return Task.CompletedTask;
        }
    }
}