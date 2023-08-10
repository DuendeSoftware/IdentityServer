// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Duende.IdentityServer.Hosting;

namespace Duende.IdentityServer.Endpoints.Results;

/// <summary>
/// Result for a discovery document
/// </summary>
/// <seealso cref="IEndpointResult" />
public class DiscoveryDocumentResult : EndpointResult<DiscoveryDocumentResult>
{
    /// <summary>
    /// Gets the entries.
    /// </summary>
    /// <value>
    /// The entries.
    /// </value>
    public Dictionary<string, object> Entries { get; }

    /// <summary>
    /// Gets the maximum age.
    /// </summary>
    /// <value>
    /// The maximum age.
    /// </value>
    public int? MaxAge { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveryDocumentResult" /> class.
    /// </summary>
    /// <param name="entries">The entries.</param>
    /// <param name="maxAge">The maximum age.</param>
    /// <exception cref="System.ArgumentNullException">entries</exception>
    public DiscoveryDocumentResult(Dictionary<string, object> entries, int? maxAge = null)
    {
        Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        MaxAge = maxAge;
    }
}

class DiscoveryDocumentResultGenerator : IEndpointResultGenerator<DiscoveryDocumentResult>
{
    /// <inheritdoc/>
    public Task ExecuteAsync(DiscoveryDocumentResult result, HttpContext context)
    {
        if (result.MaxAge.HasValue && result.MaxAge.Value >= 0)
        {
            context.Response.SetCache(result.MaxAge.Value, "Origin");
        }

        return context.Response.WriteJsonAsync(result.Entries);
    }
}