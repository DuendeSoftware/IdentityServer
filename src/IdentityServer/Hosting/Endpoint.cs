// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

#pragma warning disable 1591

using System;
using Microsoft.AspNetCore.Http;

namespace Duende.IdentityServer.Hosting;

public class Endpoint
{
    public Endpoint(string name, string path, Type handlerType)
    {
        Name = name;
        Path = path;
        Handler = handlerType;
    }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>
    /// The name.
    /// </value>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the path.
    /// </summary>
    /// <value>
    /// The path.
    /// </value>
    public PathString Path { get; set; }

    /// <summary>
    /// Gets or sets the handler.
    /// </summary>
    /// <value>
    /// The handler.
    /// </value>
    public Type Handler { get; set; }
}
