using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Duende.IdentityServer;

internal static class Metrics
{
    public static readonly Meter MyMeter = new("Duende.IdentityServer", "7.0");
    
    public static readonly Counter<long> RequestCounter = MyMeter.CreateCounter<long>("TotalRequests");
}