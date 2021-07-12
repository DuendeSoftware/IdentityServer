// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Extensions;
using IdentityModel;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Duende.IdentityServer
{
    /// <summary>
    /// Extensions for IdentityServerTools
    /// </summary>
    public static class IdentityServerToolsExtensions
    {
        /// <summary>
        /// Issues the client JWT.
        /// </summary>
        /// <param name="tools">The tools.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="lifetime">The lifetime.</param>
        /// <param name="scopes">The scopes.</param>
        /// <param name="audiences">The audiences.</param>
        /// <param name="additionalClaims">Additional claims</param>
        /// <returns></returns>
        public static async Task<string> IssueClientJwtAsync(this IdentityServerTools tools,
            string clientId,
            int lifetime,
            IEnumerable<string> scopes = null,
            IEnumerable<string> audiences = null,
            IEnumerable<Claim> additionalClaims = null)
        {
            var claims = new HashSet<Claim>(new ClaimComparer());
            var options = tools.ServiceProvider.GetRequiredService<IdentityServerOptions>();

            if (additionalClaims != null)
            {
                foreach (var claim in additionalClaims)
                {
                    claims.Add(claim);
                }
            }

            claims.Add(new Claim(JwtClaimTypes.ClientId, clientId));

            if (!scopes.IsNullOrEmpty())
            {
                foreach (var scope in scopes)
                {
                    claims.Add(new Claim(JwtClaimTypes.Scope, scope));
                }
            }

            if (options.EmitStaticAudienceClaim)
            {
                claims.Add(new Claim(
                    JwtClaimTypes.Audience,
                    string.Format(IdentityServerConstants.AccessTokenAudience, (await tools.IssuerNameService.GetCurrentAsync()).EnsureTrailingSlash())));
            }

            if (!audiences.IsNullOrEmpty())
            {
                foreach (var audience in audiences)
                {
                    claims.Add(new Claim(JwtClaimTypes.Audience, audience));
                }
            }

            return await tools.IssueJwtAsync(lifetime, claims);
        }
    }
}