// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Services;

namespace IntegrationTests.TestFramework;

public class MockCancellationTokenProvider : ICancellationTokenProvider
{
    public CancellationToken CancellationToken => CancellationToken.None;
}
