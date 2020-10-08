// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;

namespace Duende.IdentityServer.Services
{
    /// <summary>
    /// Default token creation service
    /// </summary>
    public class DefaultTokenCreationService : ITokenCreationService
    {
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

        /// <summary>
        /// Creates the token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns>
        /// A protected and serialized security token
        /// </returns>
        public virtual async Task<string> CreateTokenAsync(Token token)
        {
            var header = await CreateHeaderAsync(token);
            var payload = await CreatePayloadAsync(token);

            return await CreateJwtAsync(new JwtSecurityToken(header, payload));
        }

        /// <summary>
        /// Creates the JWT header
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns>The JWT header</returns>
        protected virtual async Task<JwtHeader> CreateHeaderAsync(Token token)
        {
            var credential = await Keys.GetSigningCredentialsAsync(token.AllowedSigningAlgorithms);

            if (credential == null)
            {
                throw new InvalidOperationException("No signing credential is configured. Can't create JWT token");
            }

            var header = new JwtHeader(credential);

            // emit x5t claim for backwards compatibility with v4 of MS JWT library
            if (credential.Key is X509SecurityKey x509Key)
            {
                var cert = x509Key.Certificate;
                if (Clock.UtcNow.UtcDateTime > cert.NotAfter)
                {
                    Logger.LogWarning("Certificate {subjectName} has expired on {expiration}", cert.Subject, cert.NotAfter.ToString(CultureInfo.InvariantCulture));
                }

                header["x5t"] = Base64Url.Encode(cert.GetCertHash());
            }

            if (token.Type == IdentityServerConstants.TokenTypes.AccessToken)
            {
                if (Options.AccessTokenJwtType.IsPresent())
                {
                    header["typ"] = Options.AccessTokenJwtType;
                }
            }

            return header;
        }

        /// <summary>
        /// Creates the JWT payload
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns>The JWT payload</returns>
        protected virtual Task<JwtPayload> CreatePayloadAsync(Token token)
        {
            var payload = token.CreateJwtPayload(Clock, Options, Logger);
            return Task.FromResult(payload);
        }

        /// <summary>
        /// Applies the signature to the JWT
        /// </summary>
        /// <param name="jwt">The JWT object.</param>
        /// <returns>The signed JWT</returns>
        protected virtual Task<string> CreateJwtAsync(JwtSecurityToken jwt)
        {
            var handler = new JwtSecurityTokenHandler();
            return Task.FromResult(handler.WriteToken(jwt));
        }
    }
}