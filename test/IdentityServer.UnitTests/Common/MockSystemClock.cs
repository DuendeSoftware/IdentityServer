// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer;
using System;

namespace UnitTests.Common;

class MockSystemClock : IClock
{
    public DateTimeOffset Now { get; set; }

    public DateTimeOffset UtcNow
    {
        get
        {
            return Now;
        }
    }
}