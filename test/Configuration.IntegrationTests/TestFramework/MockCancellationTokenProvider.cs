using Duende.IdentityServer.Services;

namespace IntegrationTests.TestFramework;

public class MockCancellationTokenProvider : ICancellationTokenProvider
{
    public CancellationToken CancellationToken => CancellationToken.None;
}