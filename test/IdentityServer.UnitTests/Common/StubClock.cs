// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using Microsoft.AspNetCore.Authentication;

namespace UnitTests.Common;

internal class StubClock : ISystemClock
{
    public Func<DateTime> UtcNowFunc { get; set; } = () => DateTime.UtcNow;
    public DateTimeOffset UtcNow => new DateTimeOffset(UtcNowFunc());
}