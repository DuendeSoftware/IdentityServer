// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Stores;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Configuration.DependencyInjection;

namespace Duende.IdentityServer.Services;

/// <summary>
/// Decorator on the refresh token service to extend server side sessions when refresh tokens are used.
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
    protected readonly IdentityServerOptions IdentityServerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRefreshTokenService" /> class.
    /// </summary>
    public ServerSideSessionRefreshTokenService(
        Decorator<IRefreshTokenService> inner,
        IdentityServerOptions identityServerOptions,
        ILogger<DefaultRefreshTokenService> logger, 
        IServerSideTicketService serverSideTicketStore)
    {
        Inner = inner.Instance;
        IdentityServerOptions = identityServerOptions;

        Logger = logger;
        ServerSideTicketStore = serverSideTicketStore;
    }

    /// <inheritdoc/>
    public Task<TokenValidationResult> ValidateRefreshTokenAsync(string tokenHandle, Client client)
    {
        return Inner.ValidateRefreshTokenAsync(tokenHandle, client);
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
        var extendSession =
            request.Client.ActivityExtendsServerSideSession == true ||
            (IdentityServerOptions.ServerSideSessions.ClientActivityExtendsServerSideSession && request.Client.ActivityExtendsServerSideSession != false);

        if (extendSession)
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
