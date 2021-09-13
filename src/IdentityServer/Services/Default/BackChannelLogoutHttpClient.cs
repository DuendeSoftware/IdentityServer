// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Services
{
    /// <summary>
    /// Models making HTTP requests for back-channel logout notification.
    /// </summary>
    public class DefaultBackChannelLogoutHttpClient : IBackChannelLogoutHttpClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<DefaultBackChannelLogoutHttpClient> _logger;
        private readonly ICancellationTokenService _cancellationTokenService;

        /// <summary>
        /// Constructor for BackChannelLogoutHttpClient.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="cancellationTokenService"></param>
        public DefaultBackChannelLogoutHttpClient(HttpClient client, ILoggerFactory loggerFactory, ICancellationTokenService cancellationTokenService = null)
        {
            _client = client;
            _logger = loggerFactory.CreateLogger<DefaultBackChannelLogoutHttpClient>();
            _cancellationTokenService = cancellationTokenService;
        }

        /// <summary>
        /// Posts the payload to the url.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public async Task PostAsync(string url, Dictionary<string, string> payload)
        {
            try
            {
                var response = await _client.PostAsync(url, new FormUrlEncodedContent(payload), _cancellationTokenService?.CancellationToken ?? default);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Response from back-channel logout endpoint: {url} status code: {status}", url, (int)response.StatusCode);
                }
                else
                {
                    _logger.LogWarning("Response from back-channel logout endpoint: {url} status code: {status}", url, (int)response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception invoking back-channel logout for url: {url}", url);
            }
        }
    }
}