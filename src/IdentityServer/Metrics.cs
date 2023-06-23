using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Duende.IdentityServer;

internal static class Metrics
{
    public static readonly Meter Meter = new(Tracing.TraceNames.Basic, Tracing.ServiceVersion);
    
    public static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>("ProtocolRequests");
}