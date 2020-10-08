using Microsoft.AspNetCore.Authentication;
using System;

namespace UnitTests.Common
{
    class MockSystemClock : ISystemClock
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
}
