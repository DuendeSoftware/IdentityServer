// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.Bff.Endpoints;

namespace Duende.Bff.Tests.TestInfra;

/// <summary>
/// Test double that records that the <see cref="IReturnUrlValidator"/> extension point was
/// invoked and returns a caller-supplied verdict, so endpoints can be verified to honor the
/// registered validator rather than their own built-in local-URL heuristic.
/// </summary>
internal sealed class RecordingReturnUrlValidator : IReturnUrlValidator
{
    private readonly bool _result;

    internal RecordingReturnUrlValidator(bool result) => _result = result;

    internal bool WasCalled { get; private set; }
    internal Uri? ReturnUrl { get; private set; }

    public Task<bool> IsValidAsync(Uri returnUrl, Ct ct = default)
    {
        WasCalled = true;
        ReturnUrl = returnUrl;
        return Task.FromResult(_result);
    }
}
