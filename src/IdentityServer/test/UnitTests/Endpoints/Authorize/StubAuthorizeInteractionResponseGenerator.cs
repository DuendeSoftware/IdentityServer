// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Validation;

namespace UnitTests.Endpoints.Authorize
{
    internal class StubAuthorizeInteractionResponseGenerator : IAuthorizeInteractionResponseGenerator
    {
        internal InteractionResponse Response { get; set; } = new InteractionResponse();

        public Task<InteractionResponse> ProcessInteractionAsync(ValidatedAuthorizeRequest request, ConsentResponse consent = null)
        {
            return Task.FromResult(Response);
        }
    }
}