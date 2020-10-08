// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;
using IdentityServer4.Services;

namespace IdentityServer.UnitTests.Common
{
    public class StubHandleGenerationService : DefaultHandleGenerationService, IHandleGenerationService
    {
        public string Handle { get; set; }

        public new Task<string> GenerateAsync(int length)
        {
            if (Handle != null) return Task.FromResult(Handle);
            return base.GenerateAsync(length);
        }
    }
}
