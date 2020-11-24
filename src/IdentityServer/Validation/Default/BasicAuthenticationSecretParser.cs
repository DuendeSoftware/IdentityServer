// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using IdentityModel;
using Microsoft.AspNetCore.Http;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Parses a Basic Authentication header
    /// </summary>
    public class BasicAuthenticationSecretParser : ISecretParser
    {
        private readonly ILogger _logger;
        private readonly IdentityServerOptions _options;

        /// <summary>
        /// Creates the parser with a reference to identity server options
        /// </summary>
        /// <param name="options">IdentityServer options</param>
        /// <param name="logger">The logger</param>
        public BasicAuthenticationSecretParser(IdentityServerOptions options, ILogger<BasicAuthenticationSecretParser> logger)
        {
            _options = options;
            _logger = logger;
        }

        /// <summary>
        /// Returns the authentication method name that this parser implements
        /// </summary>
        /// <value>
        /// The authentication method.
        /// </value>
        public string AuthenticationMethod => OidcConstants.EndpointAuthenticationMethods.BasicAuthentication;

        /// <summary>
        /// Tries to find a secret that can be used for authentication
        /// </summary>
        /// <returns>
        /// A parsed secret
        /// </returns>
        public Task<ParsedSecret> ParseAsync(HttpContext context)
        {
            _logger.LogDebug("Start parsing Basic Authentication secret");

            var notfound = Task.FromResult<ParsedSecret>(null);
            var authorizationHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (authorizationHeader.IsMissing())
            {
                return notfound;
            }

            if (!authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                return notfound;
            }

            var parameter = authorizationHeader.Substring("Basic ".Length);

            string pair;
            try
            {
                pair = Encoding.UTF8.GetString(
                    Convert.FromBase64String(parameter));
            }
            catch (FormatException)
            {
                _logger.LogWarning("Malformed Basic Authentication credential.");
                return notfound;
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("Malformed Basic Authentication credential.");
                return notfound;
            }

            var ix = pair.IndexOf(':');
            if (ix == -1)
            {
                _logger.LogWarning("Malformed Basic Authentication credential.");
                return notfound;
            }

            var clientId = pair.Substring(0, ix);
            var secret = pair.Substring(ix + 1);

            if (clientId.IsPresent())
            {
                if (clientId.Length > _options.InputLengthRestrictions.ClientId)
                {
                    _logger.LogError("Client ID exceeds maximum length.");
                    return notfound;
                }

                if (secret.IsPresent())
                {
                    if (secret.Length > _options.InputLengthRestrictions.ClientSecret)
                    {
                        _logger.LogError("Client secret exceeds maximum length.");
                        return notfound;
                    }

                    var parsedSecret = new ParsedSecret
                    {
                        Id = Decode(clientId),
                        Credential = Decode(secret),
                        Type = IdentityServerConstants.ParsedSecretTypes.SharedSecret
                    };

                    return Task.FromResult(parsedSecret);
                }
                else
                {
                    // client secret is optional
                    _logger.LogDebug("client id without secret found");

                    var parsedSecret = new ParsedSecret
                    {
                        Id = Decode(clientId),
                        Type = IdentityServerConstants.ParsedSecretTypes.NoSecret
                    };

                    return Task.FromResult(parsedSecret);
                }
            }

            _logger.LogDebug("No Basic Authentication secret found");
            return notfound;
        }

        // RFC6749 says individual values must be application/x-www-form-urlencoded
        // 2.3.1
        private string Decode(string value)
        {
            if (value.IsMissing()) return string.Empty;

            return Uri.UnescapeDataString(value.Replace("+", "%20"));
        }
    }
}