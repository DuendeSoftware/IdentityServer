using Duende.IdentityServer;
using System;

namespace UnitTests.Services.Default.KeyManagement;

class MockClock : IClock
{
    public MockClock()
    {
    }

    public MockClock(DateTime now)
    {
        UtcNow = now;
    }

    public DateTimeOffset UtcNow { get; set; }
}