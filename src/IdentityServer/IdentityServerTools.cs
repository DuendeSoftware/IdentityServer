// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

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

    /// <summary>
    /// Service Provider to resolve services.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Issuer name service
    /// </summary>
    public IIssuerNameService IssuerNameService { get; }
}

/// <summary>
/// Class for useful helpers for interacting with IdentityServer
/// </summary>
public class IdentityServerTools : IIdentityServerTools
{
    /// <inheritdoc/>
    public IServiceProvider ServiceProvider { get; }

    /// <inheritdoc/>
    public IIssuerNameService IssuerNameService { get; }

    private readonly ITokenCreationService _tokenCreation;
    private readonly IClock _clock;

    /// <inheritdoc/>
    public IdentityServerTools(IServiceProvider serviceProvider, IIssuerNameService issuerNameService, ITokenCreationService tokenCreation, IClock clock)
    {
        ServiceProvider = serviceProvider;
        IssuerNameService = issuerNameService;
        _tokenCreation = tokenCreation;
        _clock = clock;
    }

    /// <inheritdoc/>
    public virtual async Task<string> IssueJwtAsync(int lifetime, IEnumerable<Claim> claims)
    {
        var issuer = await IssuerNameService.GetCurrentAsync();
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
}
