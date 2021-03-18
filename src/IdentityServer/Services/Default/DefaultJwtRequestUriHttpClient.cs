// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using Duende.IdentityServer.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using IdentityModel;

namespace Duende.IdentityServer.Services
{
    /// <summary>
    /// Default JwtRequest client
    /// </summary>
    public class DefaultJwtRequestUriHttpClient : IJwtRequestUriHttpClient
    {
        private readonly HttpClient _client;
        private readonly IdentityServerOptions _options;
        private readonly ILogger<DefaultJwtRequestUriHttpClient> _logger;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="client">An HTTP client</param>
        /// <param name="options">The options.</param>
        /// <param name="loggerFactory">The logger factory</param>
        public DefaultJwtRequestUriHttpClient(HttpClient client, IdentityServerOptions options, ILoggerFactory loggerFactory)
        {
            _client = client;
            _options = options;
            _logger = loggerFactory.CreateLogger<DefaultJwtRequestUriHttpClient>();
        }


        /// <inheritdoc />
        public async Task<string> GetJwtAsync(string url, Client client)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, url);

#if NET5_0
            req.Options.TryAdd(IdentityServerConstants.JwtRequestClientKey, client);
#elif NETCOREAPP3_1
            req.Properties.Add(IdentityServerConstants.JwtRequestClientKey, client);
#endif
         
            var response = await _client.SendAsync(req);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                if (_options.StrictJarValidation)
                {
                    if (!string.Equals(response.Content.Headers.ContentType.MediaType,
                        $"application/{JwtClaimTypes.JwtTypes.AuthorizationRequest}", StringComparison.Ordinal))
                    {
                        _logger.LogError("Invalid content type {type} from jwt url {url}", response.Content.Headers.ContentType.MediaType, url);
                        return null;
                    }
                }

                _logger.LogDebug("Success http response from jwt url {url}", url);
                
                var json = await response.Content.ReadAsStringAsync();
                return json;
            }
                
            _logger.LogError("Invalid http status code {status} from jwt url {url}", response.StatusCode, url);
            return null;
        }
    }
}
