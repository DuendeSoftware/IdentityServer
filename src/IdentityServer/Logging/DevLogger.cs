using System;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Logging;

#pragma warning disable 1591

/// <summary>
/// More efficient logging for debug/traces
/// </summary>
/// <typeparam name="T"></typeparam>
public class DevLogger<T> : IDevLogger<T>
{
    private readonly ILogger<T> _logger;
    
    public DevLogger(ILogger<T> logger)
    {
        _logger = logger;
    }
    
    public void DevLogDebug(string message)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(message);
        }
    }

    public void DevLogDebug<T0>(string message, T0 arg0)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(message, arg0);
        }
    }

    public void DevLogDebug<T0, T1>(string message, T0 arg0, T1 arg1)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(message, arg0, arg1);
        }
    }

    public void DevLogDebug<T0, T1, T2>(string message, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(message, arg0, arg1, arg2);
        }
    }

    public void DevLogDebug<T0, T1, T2, T3>(string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(message, arg0, arg1, arg2, arg3);
        }
    }
    
    public void DevLogTrace(string message)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace(message);
        }
    }

    public void DevLogTrace<T0>(string message, T0 arg0)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace(message, arg0);
        }
    }

    public void DevLogTrace<T0, T1>(string message, T0 arg0, T1 arg1)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace(message, arg0, arg1);
        }
    }

    public void DevLogTrace<T0, T1, T2>(string message, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace(message, arg0, arg1, arg2);
        }
    }

    public void DevLogTrace<T0, T1, T2, T3>(string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace(message, arg0, arg1, arg2, arg3);
        }
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return _logger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        _logger.Log(logLevel, eventId, state, exception, formatter);
    }
}