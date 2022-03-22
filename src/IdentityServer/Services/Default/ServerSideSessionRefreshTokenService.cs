// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Stores;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Configuration.DependencyInjection;
using IdentityModel;
using System.Linq;
using System;

namespace Duende.IdentityServer.Services;

/// <summary>
/// Decorator on the refresh token service to coordinate refresh token lifetimes and server-side sessions.
/// </summary>
class ServerSideSessionRefreshTokenService : IRefreshTokenService
{
    /// <summary>
    /// The inner IRefreshTokenService implementation.
    /// </summary>
    protected readonly IRefreshTokenService Inner;

    /// <summary>
    /// The logger
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// The server-side ticket store, if configured.
    /// </summary>
    protected readonly IServerSideTicketService ServerSideTicketStore;
    
    /// <summary>
    /// The IdentityServer options.
    /// </summary>
    protected readonly IdentityServerOptions Options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRefreshTokenService" /> class.
    /// </summary>
    public ServerSideSessionRefreshTokenService(
        Decorator<IRefreshTokenService> inner,
        IdentityServerOptions options,
        ILogger<DefaultRefreshTokenService> logger, 
        IServerSideTicketService serverSideTicketStore)
    {
        Inner = inner.Instance;
        Options = options;

        Logger = logger;
        ServerSideTicketStore = serverSideTicketStore;
    }

    /// <inheritdoc/>
    public async Task<TokenValidationResult> ValidateRefreshTokenAsync(string tokenHandle, Client client)
    {
        var result = await Inner.ValidateRefreshTokenAsync(tokenHandle, client);

        result = await ValidateServerSideSessionAsync(result);

        return result;
    }

    /// <summary>
    /// Contains the logic to extend the server-side session for the request.
    /// </summary>
    protected virtual async Task<TokenValidationResult> ValidateServerSideSessionAsync(TokenValidationResult result)
    {
        if (!result.IsError)
        {
            var shouldCoordinate =
                result.Client.CoordinateLifetimeWithUserSession == true ||
                (Options.Authentication.CoordinateClientLifetimesWithUserSession && result.Client.CoordinateLifetimeWithUserSession != false);

            if (shouldCoordinate)
            {
                var sessions = await ServerSideTicketStore.GetSessionsAsync(new SessionFilter
                {
                    SubjectId = result.RefreshToken.SubjectId,
                    SessionId = result.RefreshToken.SessionId
                });

                var valid = sessions.Count > 0 &&
                    sessions.Any(x => x.Expires == null || DateTime.UtcNow < x.Expires.Value);

                if (!valid)
                {
                    Logger.LogDebug("Failing refresh token validation due to missing/expired server-side session for subject id {subjectId} and session id {sessionId}", result.RefreshToken.SubjectId, result.RefreshToken.SessionId);

                    result = new TokenValidationResult
                    {
                        IsError = true, Error = OidcConstants.TokenErrors.InvalidGrant
                    };
                }
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public Task<string> CreateRefreshTokenAsync(RefreshTokenCreationRequest request)
    {
        return Inner.CreateRefreshTokenAsync(request);
    }

    /// <inheritdoc/>
    public async Task<string> UpdateRefreshTokenAsync(RefreshTokenUpdateRequest request)
    {
        var result = await Inner.UpdateRefreshTokenAsync(request);

        await ExtendServerSideSessionAsync(request);

        return result;
    }

    /// <summary>
    /// Contains the logic to extend the server-side session for the request.
    /// </summary>
    protected virtual async Task ExtendServerSideSessionAsync(RefreshTokenUpdateRequest request)
    {
        // extend the session is the client is explicitly configured for it,
        // or if the global setting is enabled and the client isn't explicitly opted out (it's a bool? value)
        var shouldCoordinate =
            request.Client.CoordinateLifetimeWithUserSession == true ||
            (Options.Authentication.CoordinateClientLifetimesWithUserSession && request.Client.CoordinateLifetimeWithUserSession != false);

        if (shouldCoordinate)
        {
            Logger.LogDebug("Attempting to extend server-side session for subject id {subjectId} and session id {sessionId}", request.RefreshToken.SubjectId, request.RefreshToken.SessionId);

            await ServerSideTicketStore.ExtendSessionAsync(new SessionFilter
            {
                SubjectId = request.RefreshToken.SubjectId,
                SessionId = request.RefreshToken.SessionId
            });
        }
    }
}
