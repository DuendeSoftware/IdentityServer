// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using IdentityModel;
using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.ResponseHandling;

namespace Duende.IdentityServer.Endpoints.Results;

internal class TokenResult : IEndpointResult
{
    public TokenResponse Response { get; set; }

    public TokenResult(TokenResponse response)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
    }

    public async Task ExecuteAsync(HttpContext context)
    {
        context.Response.SetNoCache();

        if (Response.DPoPNonce.IsPresent())
        {
            context.Response.Headers[OidcConstants.HttpHeaders.DPoPNonce] = Response.DPoPNonce;
        }

        var dto = new ResultDto
        {
            id_token = Response.IdentityToken,
            access_token = Response.AccessToken,
            refresh_token = Response.RefreshToken,
            expires_in = Response.AccessTokenLifetime,
            token_type = Response.AccessTokenType,
            scope = Response.Scope,
                
            Custom = Response.Custom
        };

        await context.Response.WriteJsonAsync(dto);
    }

    internal class ResultDto
    {
        public string id_token { get; set; }
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
        public string refresh_token { get; set; }
        public string scope { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> Custom { get; set; }
    }
}