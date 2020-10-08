// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;

namespace IdentityServer.UnitTests.Common
{
    public class MockConsentService : IConsentService
    {
        public bool RequiresConsentResult { get; set; }

        public Task<bool> RequiresConsentAsync(ClaimsPrincipal subject, Client client, IEnumerable<ParsedScopeValue> parsedScopes)
        {
            return Task.FromResult(RequiresConsentResult);
        }

        public ClaimsPrincipal ConsentSubject { get; set; }
        public Client ConsentClient { get; set; }
        public IEnumerable<string> ConsentScopes { get; set; }

        public Task UpdateConsentAsync(ClaimsPrincipal subject, Client client, IEnumerable<ParsedScopeValue> parsedScopes)
        {
            ConsentSubject = subject;
            ConsentClient = client;
            ConsentScopes = parsedScopes?.Select(x => x.RawValue);

            return Task.CompletedTask;
        }
    }
}
