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

public class PushedAuthorizationResult : EndpointResult<PushedAuthorizationResult>
{
    public PushedAuthorizationResponse Response { get; }

    public PushedAuthorizationResult(PushedAuthorizationResponse response)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
    }
}

internal class PushedAuthorizationResultGenerator : IEndpointResultGenerator<PushedAuthorizationResult>
{
    public async Task ExecuteAsync(PushedAuthorizationResult result, HttpContext context)
    {
        if(result.Response is PushedAuthorizationSuccess success)
        {
            context.Response.SetNoCache(); // REVIEW - Do we need cache control headers? 
            context.Response.StatusCode = (int) HttpStatusCode.Created;
            await context.Response.WriteJsonAsync(result.Response);
            // TODO - Logs and maybe an event for PAR success
        } 
        else if(result.Response is PushedAuthorizationFailure failure)
        {
            context.Response.SetNoCache(); // REVIEW - Do we need cache control headers? 
            context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
            await context.Response.WriteJsonAsync(result.Response);
            // TODO - Logs and maybe an event for PAR failures
        } 
        else
        {
            throw new Exception("Can't happen!");
        }
    }
}