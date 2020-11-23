// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Threading.Tasks;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Http;

namespace Duende.IdentityServer.Endpoints.Results
{
    internal class UserInfoResult : IEndpointResult
    {
        public Dictionary<string, object> Claims;

        public UserInfoResult(Dictionary<string, object> claims)
        {
            Claims = claims;
        }

        public async Task ExecuteAsync(HttpContext context)
        {
            context.Response.SetNoCache();
            await context.Response.WriteJsonAsync(Claims);
        }
    }
}