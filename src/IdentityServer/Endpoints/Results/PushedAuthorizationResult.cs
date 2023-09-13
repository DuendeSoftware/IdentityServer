// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.ResponseHandling;
using Microsoft.AspNetCore.Http;
using System;
using System.Text.Json.Serialization;
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
        context.Response.SetNoCache();
        var dto = new ResultDto
        {
            ExpiresIn = result.Response.ExpiresIn,
            RequestUri = result.Response.RequestUri
        };
        await context.Response.WriteJsonAsync(dto);

    }

    internal class ResultDto
    {
        [JsonPropertyName("request_uri")]
        public string RequestUri { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}