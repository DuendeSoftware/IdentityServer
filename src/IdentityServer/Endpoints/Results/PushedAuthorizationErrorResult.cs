// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.ResponseHandling;
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Endpoints.Results;

public class PushedAuthorizationErrorResult : EndpointResult<PushedAuthorizationErrorResult>
{
    public PushedAuthorizationFailure Response { get; }

    public PushedAuthorizationErrorResult(PushedAuthorizationFailure response)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
    }
}

internal class PushedAuthorizationErrorResultGenerator : IEndpointResultGenerator<PushedAuthorizationErrorResult>
{
    public async Task ExecuteAsync(PushedAuthorizationErrorResult result, HttpContext context)
    {
        context.Response.SetNoCache();
        context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
        var dto = new ResultDto
        {
            error = result.Response.Error,
            error_description = result.Response.ErrorDescription,
        };

        await context.Response.WriteJsonAsync(dto);
        // TODO - Logs and maybe an event for PAR failures
    }

    internal class ResultDto
    {
        public string error { get; set; }
        public string error_description { get; set; }
    }
}