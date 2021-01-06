// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Services
{
    /// <summary>
    /// Default token creation service
    /// </summary>
    public class DefaultTokenCreationService : ITokenCreationService
    {
        private static readonly JsonWebTokenHandler _handler;

        /// <summary>
        /// The key service
        /// </summary>
        protected readonly IKeyMaterialService Keys;

        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        ///  The clock
        /// </summary>
        protected readonly ISystemClock Clock;

        /// <summary>
        /// The options
        /// </summary>
        protected readonly IdentityServerOptions Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTokenCreationService"/> class.
        /// </summary>
        /// <param name="clock">The options.</param>
        /// <param name="keys">The keys.</param>
        /// <param name="options">The options.</param>
        /// <param name="logger">The logger.</param>
        public DefaultTokenCreationService(
            ISystemClock clock,
            IKeyMaterialService keys,
            IdentityServerOptions options,
            ILogger<DefaultTokenCreationService> logger)
        {
            Clock = clock;
            Keys = keys;
            Options = options;
            Logger = logger;
        }

        static DefaultTokenCreationService()
        {
            _handler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };
        }

        /// <summary>
        /// Creates the token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns>
        /// A protected and serialized security token
        /// </returns>
        public virtual async Task<string> CreateTokenAsync(Token token)
        {
            var payload = CreatePayloadAsync(token);
            var headerElements = CreateHeaderElementsAsync(token);

            return await CreateJwtAsync(token, await payload, await headerElements);
        }


        /// <summary>
        /// Creates the JWT payload
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
#if NET5_0
        protected virtual ValueTask<string> CreatePayloadAsync(Token token)
#endif
#if NETCOREAPP3_1
        protected virtual Task<string> CreatePayloadAsync(Token token)
#endif
        {
            var payload = token.CreateJwtPayloadDictionary(Options, Clock, Logger);
#if NET5_0
            return ValueTask.FromResult(JsonSerializer.Serialize(payload));
#endif
#if NETCOREAPP3_1
            return Task.FromResult(JsonSerializer.Serialize(payload));
#endif
        }


        /// <summary>
        /// Creates additional JWT header elements
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
#if NET5_0
        protected virtual ValueTask<Dictionary<string, object>> CreateHeaderElementsAsync(Token token)
#endif
#if NETCOREAPP3_1
        protected virtual Task<Dictionary<string, object>> CreateHeaderElementsAsync(Token token)
#endif
        {
            var additionalHeaderElements = new Dictionary<string, object>();

            if (token.Type == IdentityServerConstants.TokenTypes.AccessToken)
            {
                if (Options.AccessTokenJwtType.IsPresent())
                {
                    additionalHeaderElements.Add("typ", Options.AccessTokenJwtType);
                }
            }
#if NET5_0
            return ValueTask.FromResult(additionalHeaderElements);
#endif
#if NETCOREAPP3_1
            return Task.FromResult(additionalHeaderElements);
#endif
        }

        /// <summary>
        /// Creates JWT token
        /// </summary>
        /// <param name="token"></param>
        /// <param name="payload"></param>
        /// <param name="headerElements"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected virtual async Task<string> CreateJwtAsync(Token token, string payload,
            Dictionary<string, object> headerElements)
        {
            var credential = await Keys.GetSigningCredentialsAsync(token.AllowedSigningAlgorithms);

            if (credential == null)
            {
                throw new InvalidOperationException("No signing credential is configured. Can't create JWT token");
            }

            return _handler.CreateToken(payload, credential, headerElements);
        }
    }
}