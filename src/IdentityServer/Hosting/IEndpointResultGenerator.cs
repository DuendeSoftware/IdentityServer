// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Hosting;

/// <summary>
/// Endpoint result generator
/// </summary>
public interface IEndpointResultGenerator<in T>
    where T : IEndpointResult
{
    /// <summary>
    /// Writes the endpoint result to the HTTP response.
    /// </summary>
    Task ProcessAsync(T result, HttpContext context);
}

