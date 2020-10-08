// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.Extensions.Logging;

namespace IdentityServer.UnitTests.Common
{
    public static class TestLogger
    {
        public static ILogger<T> Create<T>()
        {
            return new LoggerFactory().CreateLogger<T>();
        }
    }
}