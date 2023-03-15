// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;

namespace Duende.IdentityServer.Events;

/// <summary>
/// The default event service
/// </summary>
/// <seealso cref="IEventService" />
public class DefaultEventService : IEventService
{
    /// <summary>
    /// The options
    /// </summary>
    protected readonly IdentityServerOptions Options;

    /// <summary>
    /// The context
    /// </summary>
    protected readonly IHttpContextAccessor Context;
    
    /// <summary>
    /// The list of available sinks
    /// </summary>
    protected readonly IEnumerable<IEventSink> Sinks;

    /// <summary>
    /// The clock
    /// </summary>
    protected readonly ISystemClock Clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultEventService"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="context">The context.</param>
    /// <param name="sinks">The sinks.</param>
    /// <param name="clock">The clock.</param>
    public DefaultEventService(IdentityServerOptions options, IHttpContextAccessor context, IEnumerable<IEventSink> sinks, ISystemClock clock)
    {
        Options = options;
        Context = context;
        Sinks = sinks;
        Clock = clock;
    }

    /// <summary>
    /// Raises the specified event.
    /// </summary>
    /// <param name="evt">The event.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException">evt</exception>
    public async Task RaiseAsync(Event evt)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));

        if (CanRaiseEvent(evt))
        {
            await PrepareEventAsync(evt);
            var sinkWriteOperations = Sinks.Select(sink => sink.PersistAsync(evt)).ToList();
            await Task.WhenAll(sinkWriteOperations);
        }
    }

    /// <summary>
    /// Indicates if the type of event will be persisted.
    /// </summary>
    /// <param name="evtType"></param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentOutOfRangeException"></exception>
    public bool CanRaiseEventType(EventTypes evtType)
    {
        switch (evtType)
        {
            case EventTypes.Failure:
                return Options.Events.RaiseFailureEvents;
            case EventTypes.Information:
                return Options.Events.RaiseInformationEvents;
            case EventTypes.Success:
                return Options.Events.RaiseSuccessEvents;
            case EventTypes.Error:
                return Options.Events.RaiseErrorEvents;
            default:
                throw new ArgumentOutOfRangeException(nameof(evtType));
        }
    }

    /// <summary>
    /// Determines whether this event would be persisted.
    /// </summary>
    /// <param name="evt">The evt.</param>
    /// <returns>
    ///   <c>true</c> if this event would be persisted; otherwise, <c>false</c>.
    /// </returns>
    protected virtual bool CanRaiseEvent(Event evt)
    {
        return CanRaiseEventType(evt.EventType);
    }

    /// <summary>
    /// Prepares the event.
    /// </summary>
    /// <param name="evt">The evt.</param>
    /// <returns></returns>
    protected virtual async Task PrepareEventAsync(Event evt)
    {
        evt.ActivityId = Context.HttpContext.TraceIdentifier;
        evt.TimeStamp = DateTime.UtcNow;
        evt.ProcessId = Process.GetCurrentProcess().Id;

        if (Context.HttpContext.Connection.LocalIpAddress != null)
        {
            evt.LocalIpAddress = Context.HttpContext.Connection.LocalIpAddress.ToString() + ":" + Context.HttpContext.Connection.LocalPort;
        }
        else
        {
            evt.LocalIpAddress = "unknown";
        }

        if (Context.HttpContext.Connection.RemoteIpAddress != null)
        {
            evt.RemoteIpAddress = Context.HttpContext.Connection.RemoteIpAddress.ToString();
        }
        else
        {
            evt.RemoteIpAddress = "unknown";
        }

        await evt.PrepareAsync();
    }
}