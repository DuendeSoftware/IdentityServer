using System;
using System.Diagnostics;

namespace Duende.IdentityServer;

public static class Tracing
{
    private static readonly ActivitySource _source = new(
        ServiceName,
        Version);

    public static ActivitySource ActivitySource => _source;

    public static string ServiceName => "Duende.IdentityServer";
    public static string Version => "6.0.0";
}