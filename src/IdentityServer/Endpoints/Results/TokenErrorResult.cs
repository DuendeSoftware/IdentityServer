// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Duende.IdentityServer.Extensions;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.ResponseHandling;
using IdentityModel;

namespace Duende.IdentityServer.Endpoints.Results;

internal class TokenErrorResult : IEndpointResult
{
    public TokenErrorResponse Response { get; }

    public TokenErrorResult(TokenErrorResponse error)
    {
        if (error.Error.IsMissing()) throw new ArgumentNullException(nameof(error.Error), "Error must be set");

        Response = error;
    }

    public async Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = 400;
        context.Response.SetNoCache();

        if (Response.DPoPNonce.IsPresent())
        {
            context.Response.Headers[OidcConstants.HttpHeaders.DPoPNonce] = Response.DPoPNonce;
        }

        var dto = new ResultDto
        {
            error = Response.Error,
            error_description = Response.ErrorDescription,
                
            custom = Response.Custom
        };

        await context.Response.WriteJsonAsync(dto);
    }

    internal class ResultDto
    {
        public string error { get; set; }
        public string error_description { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> custom { get; set; }
    }    
}