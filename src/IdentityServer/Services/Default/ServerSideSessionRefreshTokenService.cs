// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Duende.IdentityServer.Configuration.DependencyInjection;

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
    /// The session coordination service.
    /// </summary>
    protected readonly ISessionCoordinationService SessionCoordinationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRefreshTokenService" /> class.
    /// </summary>
    public ServerSideSessionRefreshTokenService(
        Decorator<IRefreshTokenService> inner,
        ISessionCoordinationService sessionCoordinationService)
    {
        Inner = inner.Instance;
        SessionCoordinationService = sessionCoordinationService;
    }

    /// <inheritdoc/>
    public async Task<TokenValidationResult> ValidateRefreshTokenAsync(string tokenHandle, Client client)
    {
        var result = await Inner.ValidateRefreshTokenAsync(tokenHandle, client);

        if (!result.IsError)
        {
            result = await SessionCoordinationService.ValidateRefreshTokenAsync(result);
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

        await SessionCoordinationService.ProcessRefreshTokenUpdateAsync(request);

        return result;
    }
}
