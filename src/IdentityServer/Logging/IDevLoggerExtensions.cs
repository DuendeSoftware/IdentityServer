using System;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Logging;

public static partial class IDevLoggerExtensions
{
    [LoggerMessage(EventId = 0, EventName = "UnhandledException", Level = LogLevel.Critical,
        Message = "Unhandled exception: {message}")]
    public static partial void UnhandledException(this IDevLogger logger, string message);
    
    [LoggerMessage(EventId = 1, EventName = "InvokeEndpoint", Level = LogLevel.Information,
        Message = "Invoking IdentityServer endpoint: {endpointType} for {url}")]
    public static partial void InvokeEndpoint(this IDevLogger logger, string endpointType, string url);
}