// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;

namespace IdentityServer.UnitTests.Validation.Setup
{
    internal class TestProfileService : IProfileService
    {
        private bool _shouldBeActive;

        public TestProfileService(bool shouldBeActive = true)
        {
            _shouldBeActive = shouldBeActive;
        }

        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            return Task.CompletedTask;
        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            context.IsActive = _shouldBeActive;
            return Task.CompletedTask;
        }
    }
}