using System;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Logging;

internal static partial class IDevLoggerExtensions
{
    [LoggerMessage(EventId = 0, EventName = "UnhandledException", Level = LogLevel.Critical,
        Message = "Unhandled exception")]
    internal static partial void UnhandledException(this IDevLogger logger, Exception exception);
    
    [LoggerMessage(EventId = 1, EventName = "InvokeEndpoint", Level = LogLevel.Information,
        Message = "Invoking IdentityServer endpoint: {endpointType} for {url}")]
    internal static partial void InvokeEndpoint(this IDevLogger logger, string endpointType, string url);
}