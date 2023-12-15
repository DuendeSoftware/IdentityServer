// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using Duende.IdentityServer;

namespace IntegrationTests.Common;

class MockClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
}