// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Text.Json.Serialization;

namespace Duende.IdentityServer.Events;

/// <summary>
/// Event for unhandled exceptions
/// </summary>
/// <seealso cref="Event" />
public class UnhandledExceptionEvent : Event
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnhandledExceptionEvent"/> class.
    /// </summary>
    /// <param name="ex">The ex.</param>
    public UnhandledExceptionEvent(Exception ex)
        : base(EventCategories.Error,
            "Unhandled Exception",
            EventTypes.Error,
            EventIds.UnhandledException,
            ex.Message)
    {
        Exception = ex;
        Details = ex.ToString();
    }

    /// <summary>
    /// Gets or sets the details.
    /// </summary>
    /// <value>
    /// The details.
    /// </value>
    public string Details { get; set; }

    /// <summary>
    /// Gets or sets the exception.
    /// </summary>
    /// <value>
    /// The exception.
    /// </value>
    [JsonIgnore] // Don't try to serialize exceptions, because System.Text.Json will fail (it doesn't support the pointers inside a call stack)
    public Exception Exception { get; set; }
}