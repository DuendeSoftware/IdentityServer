﻿// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;

namespace Duende.IdentityServer;

class DefaultClock : IClock
{
    private readonly TimeProvider _timeProvider;

    public DefaultClock()
    {
        _timeProvider = TimeProvider.System;
    }

    public DefaultClock(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public DateTimeOffset UtcNow { get => _timeProvider.GetUtcNow(); }
}
