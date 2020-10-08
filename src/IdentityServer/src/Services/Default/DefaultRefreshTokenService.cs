// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Stores;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using IdentityModel;
using Duende.IdentityServer.Logging.Models;
using Microsoft.AspNetCore.Authentication;

namespace Duende.IdentityServer.Services
{
    /// <summary>
    /// Default refresh token service
    /// </summary>
    public class DefaultRefreshTokenService : IRefreshTokenService
    {
        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// The refresh token store
        /// </summary>
        protected IRefreshTokenStore RefreshTokenStore { get; }

        /// <summary>
        /// The profile service
        /// </summary>
        protected IProfileService Profile { get; }

        /// <summary>
        /// The clock
        /// </summary>
        protected ISystemClock Clock { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRefreshTokenService" /> class.
        /// </summary>
        /// <param name="refreshTokenStore">The refresh token store</param>
        /// <param name="profile"></param>
        /// <param name="clock">The clock</param>
        /// <param name="logger">The logger</param>
        public DefaultRefreshTokenService(IRefreshTokenStore refreshTokenStore, IProfileService profile,
            ISystemClock clock,
            ILogger<DefaultRefreshTokenService> logger)
        {
            RefreshTokenStore = refreshTokenStore;
            Profile = profile;
            Clock = clock;

            Logger = logger;
        }

        /// <summary>
        /// Validates a refresh token
        /// </summary>
        /// <param name="tokenHandle">The token handle.</param>
        /// <param name="client">The client.</param>
        /// <returns></returns>
        public virtual async Task<TokenValidationResult> ValidateRefreshTokenAsync(string tokenHandle, Client client)
        {
            var invalidGrant = new TokenValidationResult
            {
                IsError = true, Error = OidcConstants.TokenErrors.InvalidGrant
            };

            Logger.LogTrace("Start refresh token validation");

            /////////////////////////////////////////////
            // check if refresh token is valid
            /////////////////////////////////////////////
            var refreshToken = await RefreshTokenStore.GetRefreshTokenAsync(tokenHandle);
            if (refreshToken == null)
            {
                Logger.LogWarning("Invalid refresh token");
                return invalidGrant;
            }

            /////////////////////////////////////////////
            // check if refresh token has expired
            /////////////////////////////////////////////
            if (refreshToken.CreationTime.HasExceeded(refreshToken.Lifetime, Clock.UtcNow.DateTime))
            {
                Logger.LogWarning("Refresh token has expired.");
                return invalidGrant;
            }
            
            /////////////////////////////////////////////
            // check if client belongs to requested refresh token
            /////////////////////////////////////////////
            if (client.ClientId != refreshToken.ClientId)
            {
                Logger.LogError("{0} tries to refresh token belonging to {1}", client.ClientId, refreshToken.ClientId);
                return invalidGrant;
            }

            /////////////////////////////////////////////
            // check if client still has offline_access scope
            /////////////////////////////////////////////
            if (!client.AllowOfflineAccess)
            {
                Logger.LogError("{clientId} does not have access to offline_access scope anymore", client.ClientId);
                return invalidGrant;
            }
            
            /////////////////////////////////////////////
            // check if refresh token has been consumed
            /////////////////////////////////////////////
            if (refreshToken.ConsumedTime.HasValue)
            {
                if ((await AcceptConsumedTokenAsync(refreshToken)) == false)
                {
                    Logger.LogWarning("Rejecting refresh token because it has been consumed already.");
                    return invalidGrant;
                }
            }
            
            /////////////////////////////////////////////
            // make sure user is enabled
            /////////////////////////////////////////////
            var isActiveCtx = new IsActiveContext(
                refreshToken.Subject,
                client,
                IdentityServerConstants.ProfileIsActiveCallers.RefreshTokenValidation);

            await Profile.IsActiveAsync(isActiveCtx);

            if (isActiveCtx.IsActive == false)
            {
                Logger.LogError("{subjectId} has been disabled", refreshToken.Subject.GetSubjectId());
                return invalidGrant;
            }
            
            return new TokenValidationResult
            {
                IsError = false, 
                RefreshToken = refreshToken, 
                Client = client
            };
        }

        /// <summary>
        /// Callback to decide if an already consumed token should be accepted.
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        protected virtual Task<bool> AcceptConsumedTokenAsync(RefreshToken refreshToken)
        {
            // by default we will not accept consumed tokens
            // change the behavior here to implement a time window
            // you can also implement additional revocation logic here
            return Task.FromResult(false);
        }

        /// <summary>
        /// Creates the refresh token.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="client">The client.</param>
        /// <returns>
        /// The refresh token handle
        /// </returns>
        public virtual async Task<string> CreateRefreshTokenAsync(ClaimsPrincipal subject, Token accessToken,
            Client client)
        {
            Logger.LogDebug("Creating refresh token");

            int lifetime;
            if (client.RefreshTokenExpiration == TokenExpiration.Absolute)
            {
                Logger.LogDebug("Setting an absolute lifetime: {absoluteLifetime}",
                    client.AbsoluteRefreshTokenLifetime);
                lifetime = client.AbsoluteRefreshTokenLifetime;
            }
            else
            {
                lifetime = client.SlidingRefreshTokenLifetime;
                if (client.AbsoluteRefreshTokenLifetime > 0 && lifetime > client.AbsoluteRefreshTokenLifetime)
                {
                    Logger.LogWarning(
                        "Client {clientId}'s configured " + nameof(client.SlidingRefreshTokenLifetime) +
                        " of {slidingLifetime} exceeds its " + nameof(client.AbsoluteRefreshTokenLifetime) +
                        " of {absoluteLifetime}. The refresh_token's sliding lifetime will be capped to the absolute lifetime",
                        client.ClientId, lifetime, client.AbsoluteRefreshTokenLifetime);
                    lifetime = client.AbsoluteRefreshTokenLifetime;
                }

                Logger.LogDebug("Setting a sliding lifetime: {slidingLifetime}", lifetime);
            }

            var refreshToken = new RefreshToken
            {
                CreationTime = Clock.UtcNow.UtcDateTime, Lifetime = lifetime, AccessToken = accessToken
            };

            var handle = await RefreshTokenStore.StoreRefreshTokenAsync(refreshToken);
            return handle;
        }

        /// <summary>
        /// Updates the refresh token.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="refreshToken">The refresh token.</param>
        /// <param name="client">The client.</param>
        /// <returns>
        /// The refresh token handle
        /// </returns>
        public virtual async Task<string> UpdateRefreshTokenAsync(string handle, RefreshToken refreshToken,
            Client client)
        {
            Logger.LogDebug("Updating refresh token");

            bool needsCreate = false;
            bool needsUpdate = false;

            if (client.RefreshTokenUsage == TokenUsage.OneTimeOnly)
            {
                Logger.LogDebug("Token usage is one-time only. Setting current handle as consumed, and generating new handle");

                // flag as consumed
                if (refreshToken.ConsumedTime == null)
                {
                    refreshToken.ConsumedTime = Clock.UtcNow.UtcDateTime;
                    await RefreshTokenStore.UpdateRefreshTokenAsync(handle, refreshToken);
                }

                // create new one
                needsCreate = true;
            }

            if (client.RefreshTokenExpiration == TokenExpiration.Sliding)
            {
                Logger.LogDebug("Refresh token expiration is sliding - extending lifetime");

                // if absolute exp > 0, make sure we don't exceed absolute exp
                // if absolute exp = 0, allow indefinite slide
                var currentLifetime = refreshToken.CreationTime.GetLifetimeInSeconds(Clock.UtcNow.UtcDateTime);
                Logger.LogDebug("Current lifetime: {currentLifetime}", currentLifetime.ToString());

                var newLifetime = currentLifetime + client.SlidingRefreshTokenLifetime;
                Logger.LogDebug("New lifetime: {slidingLifetime}", newLifetime.ToString());

                // zero absolute refresh token lifetime represents unbounded absolute lifetime
                // if absolute lifetime > 0, cap at absolute lifetime
                if (client.AbsoluteRefreshTokenLifetime > 0 && newLifetime > client.AbsoluteRefreshTokenLifetime)
                {
                    newLifetime = client.AbsoluteRefreshTokenLifetime;
                    Logger.LogDebug("New lifetime exceeds absolute lifetime, capping it to {newLifetime}",
                        newLifetime.ToString());
                }

                refreshToken.Lifetime = newLifetime;
                needsUpdate = true;
            }

            if (needsCreate)
            {
                // set it to null so that we save non-consumed token
                refreshToken.ConsumedTime = null;
                handle = await RefreshTokenStore.StoreRefreshTokenAsync(refreshToken);
                Logger.LogDebug("Created refresh token in store");
            }
            else if (needsUpdate)
            {
                await RefreshTokenStore.UpdateRefreshTokenAsync(handle, refreshToken);
                Logger.LogDebug("Updated refresh token in store");
            }
            else
            {
                Logger.LogDebug("No updates to refresh token done");
            }

            return handle;
        }
    }
}