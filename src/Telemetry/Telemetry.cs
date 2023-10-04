// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Endpoints.Results;
using Duende.IdentityServer.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Duende.IdentityServer;

/// <summary>
/// Constants for Telemetry
/// </summary>
public static class Telemetry
{
    private readonly static string ServiceVersion = typeof(Telemetry).Assembly.GetName().Version.ToString();
    
    /// <summary>
    /// Service name used for Duende IdentityServer.
    /// </summary>
    public const string ServiceName = "Duende.IdentityServer";

    /// <summary>
    /// Metrics configuration.
    /// </summary>
    public static class Metrics
    {
        /// <summary>
        /// Meter for IdentityServer
        /// </summary>
        public static readonly Meter Meter = new Meter(ServiceName, ServiceVersion);

        /// <summary>
        /// Counter for active requests.
        /// </summary>
        public static readonly UpDownCounter<long> RequestCounter = Meter.CreateUpDownCounter<long>("active_requests");
    }
}