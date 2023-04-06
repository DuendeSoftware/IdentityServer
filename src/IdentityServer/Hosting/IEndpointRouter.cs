// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

#nullable enable

using Microsoft.AspNetCore.Http;

namespace Duende.IdentityServer.Hosting;

/// <summary>
/// The endpoint router
/// </summary>
public interface IEndpointRouter
{
    /// <summary>
    /// Finds a matching endpoint.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns></returns>
    IEndpointHandler? Find(HttpContext context);
}