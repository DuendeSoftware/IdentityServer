using System;
using System.Diagnostics;

namespace Duende.IdentityServer;

/// <summary>
/// Constants for tracing
/// </summary>
public static class Tracing
{
    private static readonly Version AssemblyVersion = typeof(Tracing).Assembly.GetName().Version;

    /// <summary>
    /// Standard ActivitySource for IdentityServer
    /// </summary>
    public static ActivitySource ActivitySource { get; } = new(
        ServiceName,
        ServiceVersion);

    /// <summary>
    /// Service name
    /// </summary>
    public static string ServiceName => "Duende.IdentityServer";

    /// <summary>
    /// Serivce version
    /// </summary>
    public static string ServiceVersion => $"{AssemblyVersion.Major}.{AssemblyVersion.Minor}.{AssemblyVersion.Build}";

    public static class Properties
    {
        public const string EndpointType = "endpoint_type";

        public const string ClientId = "client_id";
        public const string GrantType = "grant_type";
        public const string Scope = "scope";
    }
}