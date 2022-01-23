using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer;

internal static class ILoggerDevExtensions
{
    public static void DevLogTrace(this ILogger _logger, string message)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace(message);
        }
    }

    public static void DevLogTrace<T0>(this ILogger _logger, string message, T0 arg0)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace(message, arg0);
        }
    }

    public static void DevLogTrace<T0, T1>(this ILogger _logger, string message, T0 arg0, T1 arg1)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace(message, arg0, arg1);
        }
    }

    public static void DevLogTrace<T0, T1, T2>(this ILogger _logger, string message, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace(message, arg0, arg1, arg2);
        }
    }

    public static void DevLogTrace<T0, T1, T2, T3>(this ILogger _logger, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace(message, arg0, arg1, arg2, arg3);
        }
    }
    
    public static void DevLogDebug(this ILogger logger, string message)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(message);
        }
    }

    public static void DevLogDebug<T0>(this ILogger logger, string message, T0 arg0)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(message, arg0);
        }
    }

    public static void DevLogDebug<T0, T1>(this ILogger logger, string message, T0 arg0, T1 arg1)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(message, arg0, arg1);
        }
    }
    
    // todo: understand ordering of extension method visibility
    // https://codeblog.jonskeet.uk/2010/11/03/using-extension-method-resolution-rules-to-decorate-awaiters/
    // public static void LogDebug<T0, T1>(this ILogger logger, string message, T0 arg0, T1 arg1)
    // {
    //     if (logger.IsEnabled(LogLevel.Debug))
    //     {
    //         logger.LogDebug(message, arg0, arg1);
    //     }
    // }

    public static void DevLogDebug<T0, T1, T2>(this ILogger logger, string message, T0 arg0, T1 arg1, T2 arg2)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(message, arg0, arg1, arg2);
        }
    }

    public static void DevLogDebug<T0, T1, T2, T3>(this ILogger logger, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(message, arg0, arg1, arg2, arg3);
        }
    }
}