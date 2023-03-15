using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;

namespace Duende.IdentityServer.Services;

/// <summary>
/// Windows EventLog implementation of the event service. Write events raised to the windows log.
/// </summary>
public class EventLogEventSink : IEventSink
{
    private readonly WindowsEventLogOptions _windowsEventLogOptions;

    /// <summary>
    /// The EventLog event sink for windows
    /// </summary>
    /// <param name="identityServerOptions"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public EventLogEventSink(IdentityServerOptions identityServerOptions)
    {
        _windowsEventLogOptions = identityServerOptions?.Events?.WindowsEventLog ??
                                  throw new ArgumentNullException(nameof(WindowsEventLogOptions));
    }

    /// <summary>
    /// Raises the specified event and writes it to the windows event log.
    /// </summary>
    /// <param name="evt">The event.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException">evt</exception>
    [SupportedOSPlatform("windows")]
    public async Task PersistAsync(Event evt)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));

        if (!EventLog.SourceExists(_windowsEventLogOptions.Source))
        {
            EventLog.CreateEventSource(_windowsEventLogOptions.Source, string.Empty);
            // if the event log does not exist yet, wait a period of time to ensure latency won't affect.
            await Task.Delay(1000);
        }

        using var eventLog = new EventLog();
        var type = evt.EventType switch
        {
            EventTypes.Failure =>  EventLogEntryType.Error,
            EventTypes.Success => EventLogEntryType.Information,
            EventTypes.Error => EventLogEntryType.Error,
            EventTypes.Information => EventLogEntryType.Information,
            _ => EventLogEntryType.Information
        };

        eventLog.Source = _windowsEventLogOptions.Source.IsPresent()
            ? _windowsEventLogOptions.Source
            : throw new ArgumentNullException(nameof(_windowsEventLogOptions.Source));
        eventLog.WriteEntry($"{evt}", type);
    }
}