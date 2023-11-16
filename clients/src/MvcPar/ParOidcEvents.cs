using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace MvcPar
{
    public class ParOidcEvents(HttpClient httpClient, IDiscoveryCache discoveryCache, ILogger<ParOidcEvents> logger) : OpenIdConnectEvents
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly IDiscoveryCache _discoveryCache = discoveryCache;
        private readonly ILogger<ParOidcEvents> _logger = logger;

        public override async Task RedirectToIdentityProvider(RedirectContext context)
        {
            var clientId = context.ProtocolMessage.ClientId;

            // Construct the state parameter and add it to the protocol message
            // so that we include it in the pushed authorization request
            SetStateParameterForParRequest(context);

            // Make the actual pushed authorization request
            var parResponse = await PushAuthorizationParameters(context, clientId);

            // Now replace the parameters that would normally be sent to the
            // authorize endpoint with just the client id and PAR request uri.
            SetAuthorizeParameters(context, clientId, parResponse);

            // Mark the request as handled, because we don't want the normal
            // behavior that attaches state to the outgoing request (we already
            // did that in the PAR request). 
            context.HandleResponse();

            // Finally redirect to the authorize endpoint
            await RedirectToAuthorizeEndpoint(context, context.ProtocolMessage);
        }

        private const string HeaderValueEpocDate = "Thu, 01 Jan 1970 00:00:00 GMT";
        private async Task RedirectToAuthorizeEndpoint(RedirectContext context, OpenIdConnectMessage message)
        {
            // This code is copied from the ASP.NET handler. We want most of its
            // default behavior related to redirecting to the identity provider,
            // except we already pushed the state parameter, so that is left out
            // here. See https://github.com/dotnet/aspnetcore/blob/c85baf8db0c72ae8e68643029d514b2e737c9fae/src/Security/Authentication/OpenIdConnect/src/OpenIdConnectHandler.cs#L364
            if (string.IsNullOrEmpty(message.IssuerAddress))
            {
                throw new InvalidOperationException(
                    "Cannot redirect to the authorization endpoint, the configuration may be missing or invalid.");
            }

            if (context.Options.AuthenticationMethod == OpenIdConnectRedirectBehavior.RedirectGet)
            {
                var redirectUri = message.CreateAuthenticationRequestUrl();
                if (!Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute))
                {
                    _logger.LogWarning("The redirect URI is not well-formed. The URI is: '{AuthenticationRequestUrl}'.", redirectUri);
                }

                context.Response.Redirect(redirectUri);
                return;
            }
            else if (context.Options.AuthenticationMethod == OpenIdConnectRedirectBehavior.FormPost)
            {
                var content = message.BuildFormPost();
                var buffer = Encoding.UTF8.GetBytes(content);

                context.Response.ContentLength = buffer.Length;
                context.Response.ContentType = "text/html;charset=UTF-8";

                // Emit Cache-Control=no-cache to prevent client caching.
                context.Response.Headers.CacheControl = "no-cache, no-store";
                context.Response.Headers.Pragma = "no-cache";
                context.Response.Headers.Expires = HeaderValueEpocDate;

                await context.Response.Body.WriteAsync(buffer);
                return;
            }

            throw new NotImplementedException($"An unsupported authentication method has been configured: {context.Options.AuthenticationMethod}");
        }

        private async Task<ParResponse> PushAuthorizationParameters(RedirectContext context, string clientId)
        {
            // Send our PAR request
            var requestBody = new FormUrlEncodedContent(context.ProtocolMessage.Parameters);
            _httpClient.SetBasicAuthentication(clientId, "secret");

            var disco = await _discoveryCache.GetAsync();
            if (disco.IsError)
            {
                throw new Exception(disco.Error);
            }
            var parEndpoint = disco.TryGetValue("pushed_authorization_request_endpoint").GetString();
            var response = await _httpClient.PostAsync(parEndpoint, requestBody);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("PAR failure");
            }
            return await response.Content.ReadFromJsonAsync<ParResponse>();

        }

        private static void SetAuthorizeParameters(RedirectContext context, string clientId, ParResponse parResponse)
        {
            // Remove all the parameters from the protocol message, and replace with what we got from the PAR response
            context.ProtocolMessage.Parameters.Clear();
            // Then, set client id and request uri as parameters
            context.ProtocolMessage.ClientId = clientId;
            context.ProtocolMessage.RequestUri = parResponse.RequestUri;
        }

        private static OpenIdConnectMessage SetStateParameterForParRequest(RedirectContext context)
        {
            // Construct State, we also need that (this chunk copied from the OIDC handler)
            var message = context.ProtocolMessage;
            // When redeeming a code for an AccessToken, this value is needed
            context.Properties.Items.Add(OpenIdConnectDefaults.RedirectUriForCodePropertiesKey, message.RedirectUri);
            message.State = context.Options.StateDataFormat.Protect(context.Properties);
            return message;
        }

        public override Task TokenResponseReceived(TokenResponseReceivedContext context)
        {
            return base.TokenResponseReceived(context);
        }

        private class ParResponse
        {
            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonPropertyName("request_uri")]
            public string RequestUri { get; set; }
        }
    }
}