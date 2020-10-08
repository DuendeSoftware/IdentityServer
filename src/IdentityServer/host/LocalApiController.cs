// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using static IdentityServer4.IdentityServerConstants;

namespace IdentityServerHost
{
    [Route("localApi")]
    [Authorize(LocalApi.PolicyName)]
    public class LocalApiController : ControllerBase
    {
        public IActionResult Get()
        {
            var claims = from c in User.Claims select new { c.Type, c.Value };
            return new JsonResult(claims);
        }
    }
}