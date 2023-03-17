// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;

namespace Duende.IdentityServer.Configuration;

/// <summary>
/// Extension methods for adding IdentityServer configuration endpoints.
/// </summary>
public static class ConfigurationEndpointExtensions
{
    /// <summary>
    /// Maps the dynamic client registration endpoint.
    /// </summary>
    public static IEndpointConventionBuilder MapDynamicClientRegistration(this IEndpointRouteBuilder endpoints, string path = "/connect/dcr")
    {
        return endpoints.MapPost(path, (DynamicClientRegistrationEndpoint endpoint, HttpContext context) => endpoint.Process(context));
    }
}