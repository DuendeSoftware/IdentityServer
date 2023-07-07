// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.ResponseHandling;
using IdentityModel;

namespace Duende.IdentityServer.Endpoints.Results;

internal class BackchannelAuthenticationResult : IEndpointResult
{
    public BackchannelAuthenticationResponse Response { get; set; }

    public BackchannelAuthenticationResult(BackchannelAuthenticationResponse response)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
    }

    public async Task ExecuteAsync(HttpContext context)
    {
        context.Response.SetNoCache();

        if (Response.IsError)
        {
            switch (Response.Error)
            {
                case OidcConstants.BackchannelAuthenticationRequestErrors.InvalidClient:
                    context.Response.StatusCode = 401;
                    break;
                case OidcConstants.BackchannelAuthenticationRequestErrors.AccessDenied:
                    context.Response.StatusCode = 403;
                    break;
                default:
                    context.Response.StatusCode = 400;
                    break;
            }

            await context.Response.WriteJsonAsync(new ErrorResultDto { 
                error = Response.Error,
                error_description = Response.ErrorDescription
            });
        }
        else
        {
            context.Response.StatusCode = 200;
            await context.Response.WriteJsonAsync(new SuccessResultDto
            {
                auth_req_id = Response.AuthenticationRequestId,
                expires_in = Response.ExpiresIn,
                interval = Response.Interval,
                
                Custom = Response.Custom
            });
        }
    }

    internal class SuccessResultDto
    {
#pragma warning disable IDE1006 // Naming Styles
        public string auth_req_id { get; set; }
        public int expires_in { get; set; }
        public int interval { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> Custom { get; set; }
#pragma warning restore IDE1006 // Naming Styles
    }

    internal class ErrorResultDto
    {
#pragma warning disable IDE1006 // Naming Styles
        public string error { get; set; }
        public string error_description { get; set; }
#pragma warning restore IDE1006 // Naming Styles
    }
}