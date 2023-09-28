// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.ResponseHandling;
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Endpoints.Results;

public class PushedAuthorizationResult : EndpointResult<PushedAuthorizationResult>
{
    public PushedAuthorizationSuccess Response { get; }

    public PushedAuthorizationResult(PushedAuthorizationSuccess response)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
    }
}

internal class PushedAuthorizationResultGenerator : IEndpointResultGenerator<PushedAuthorizationResult>
{
    public async Task ExecuteAsync(PushedAuthorizationResult result, HttpContext context)
    {
        context.Response.SetNoCache();
        context.Response.StatusCode = (int) HttpStatusCode.Created;
        var dto = new ResultDto
        {
            request_uri = result.Response.RequestUri,
            expires_in = result.Response.ExpiresIn
        };
        await context.Response.WriteJsonAsync(dto);
        // TODO - Logs and maybe an event for PAR success
    }

    internal class ResultDto
    {
        public required string request_uri { get; set; }
        public required int expires_in { get; set; }
    }
}
