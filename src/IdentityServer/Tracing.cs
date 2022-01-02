using System;
using System.Diagnostics;

namespace Duende.IdentityServer;

public static class Tracing
{
    private static readonly ActivitySource _source = new(
        "Duende.IdentityServer",
        "6.0.0");

    public static ActivitySource ActivitySource => _source;
}