// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using IdentityModel;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Duende.IdentityServer;

/// <summary>
/// Useful helpers for interacting with IdentityServer.
/// </summary>
public interface IIdentityServerTools
{
    /// <summary>
    /// Issues a JWT.
    /// </summary>
    /// <param name="lifetime">The lifetime.</param>
    /// <param name="claims">The claims.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException">claims</exception>
    Task<string> IssueJwtAsync(int lifetime, IEnumerable<Claim> claims);

    /// <summary>
    /// Issues a JWT.
    /// </summary>
    /// <param name="lifetime">The lifetime.</param>
    /// <param name="issuer">The issuer.</param>
    /// <param name="claims">The claims.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException">claims</exception>
    Task<string> IssueJwtAsync(int lifetime, string issuer, IEnumerable<Claim> claims);

    /// <summary>
    /// Issues a JWT.
    /// </summary>
    /// <param name="lifetime">The lifetime.</param>
    /// <param name="issuer">The issuer.</param>
    /// <param name="tokenType"></param>
    /// <param name="claims">The claims.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException">claims</exception>
    Task<string> IssueJwtAsync(int lifetime, string issuer, string tokenType, IEnumerable<Claim> claims);
}

/// <summary>
/// Class for useful helpers for interacting with IdentityServer
/// </summary>
[Obsolete("Do not reference the IdentityServerTools implementation directly, use the IIdentityServerTools interface")]
public class IdentityServerTools : IIdentityServerTools
{
    private readonly IIssuerNameService _issuerNameService;
    private readonly ITokenCreationService _tokenCreation;
    private readonly IClock _clock;
    private readonly IdentityServerOptions _options;

    /// <inheritdoc/>
    public IdentityServerTools(IIssuerNameService issuerNameService, ITokenCreationService tokenCreation, IClock clock, IdentityServerOptions options)
    {
        _issuerNameService = issuerNameService;
        _tokenCreation = tokenCreation;
        _clock = clock;
        _options = options;
    }

    /// <inheritdoc/>
    public virtual async Task<string> IssueJwtAsync(int lifetime, IEnumerable<Claim> claims)
    {
        var issuer = await _issuerNameService.GetCurrentAsync();
        return await IssueJwtAsync(lifetime, issuer, claims);
    }

    /// <inheritdoc/>
    public virtual Task<string> IssueJwtAsync(int lifetime, string issuer, IEnumerable<Claim> claims)
    {
        var tokenType = OidcConstants.TokenTypes.AccessToken;
        return IssueJwtAsync(lifetime, issuer, tokenType, claims);
    }

    /// <inheritdoc/>
    public virtual async Task<string> IssueJwtAsync(int lifetime, string issuer, string tokenType, IEnumerable<Claim> claims)
    {
        if (String.IsNullOrWhiteSpace(issuer)) throw new ArgumentNullException(nameof(issuer));
        if (String.IsNullOrWhiteSpace(tokenType)) throw new ArgumentNullException(nameof(tokenType));
        if (claims == null) throw new ArgumentNullException(nameof(claims));

        var token = new Token(tokenType)
        {
            CreationTime = _clock.UtcNow.UtcDateTime,
            Issuer = issuer,
            Lifetime = lifetime,

            Claims = new HashSet<Claim>(claims, new ClaimComparer())
        };
        
        return await _tokenCreation.CreateTokenAsync(token);
    }

     /// <summary>
    /// Issues the client JWT.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="lifetime">The lifetime.</param>
    /// <param name="scopes">The scopes.</param>
    /// <param name="audiences">The audiences.</param>
    /// <param name="additionalClaims">Additional claims</param>
    /// <returns></returns>
    public virtual async Task<string> IssueClientJwtAsync(
        string clientId,
        int lifetime,
        IEnumerable<string>? scopes = null,
        IEnumerable<string>? audiences = null,
        IEnumerable<Claim>? additionalClaims = null)
    {
        var claims = new HashSet<Claim>(new ClaimComparer());

        if (additionalClaims != null)
        {
            foreach (var claim in additionalClaims)
            {
                claims.Add(claim);
            }
        }

        claims.Add(new Claim(JwtClaimTypes.ClientId, clientId));

        if (!IEnumerableExtensions.IsNullOrEmpty(scopes))
        {
            foreach (var scope in scopes)
            {
                claims.Add(new Claim(JwtClaimTypes.Scope, scope));
            }
        }

        if (_options.EmitStaticAudienceClaim)
        {
            claims.Add(new Claim(
                JwtClaimTypes.Audience,
                string.Format(IdentityServerConstants.AccessTokenAudience, (await _issuerNameService.GetCurrentAsync()).EnsureTrailingSlash())));
        }

        if (!IEnumerableExtensions.IsNullOrEmpty(audiences))
        {
            foreach (var audience in audiences)
            {
                claims.Add(new Claim(JwtClaimTypes.Audience, audience));
            }
        }

        return await IssueJwtAsync(lifetime, claims);
    }
}
