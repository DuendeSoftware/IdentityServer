using Clients;
using IdentityModel;
using IdentityModel.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleCibaClient
{
    public class Program
    {
        static IDiscoveryCache _cache = new DiscoveryCache(Constants.Authority);

        public static async Task Main()
        {
            Console.Title = "Console CIBA Client";

            var loginResponse = await RequestBackchannelLoginAsync();

            var tokenResponse = await RequestTokenAsync(loginResponse);
            tokenResponse.Show();

            Console.ReadLine();
            await CallServiceAsync(tokenResponse.AccessToken);
        }

        static async Task<BackchannelLoginResponse> RequestBackchannelLoginAsync()
        {
            var disco = await _cache.GetAsync();
            if (disco.IsError) throw new Exception(disco.Error);

            var cibaEp = disco.TryGetString("backchannel_authentication_endpoint");

            var username = "alice";
            var bindingMessage = Guid.NewGuid().ToString("N").Substring(0, 10);

            var client = new HttpClient();
            var body = new Dictionary<string, string>
            {
                {"client_id", "ciba"},
                {"client_secret", "secret"},
                {"scope", "openid profile email resource1.scope1 offline_access"},
                {"login_hint", username},
                {"binding_message", bindingMessage },
            };
            
            var response = await client.PostAsync(cibaEp, new FormUrlEncodedContent(body));
            var json = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<BackchannelLoginResponse>(json);

            if (loginResponse.IsError) throw new Exception(loginResponse.error);

            Console.WriteLine($"Login Hint                  : {username}");
            Console.WriteLine($"Binding Message             : {bindingMessage}");
            Console.WriteLine($"Authentication Request Id   : {loginResponse.auth_req_id}");
            Console.WriteLine($"Expires In                  : {loginResponse.expires_in}");
            Console.WriteLine($"Interval                    : {loginResponse.interval}");
            Console.WriteLine();

            return loginResponse;
        }

        private static async Task<TokenResponse> RequestTokenAsync(BackchannelLoginResponse authorizeResponse)
        {
            var disco = await _cache.GetAsync();
            if (disco.IsError) throw new Exception(disco.Error);

            var client = new HttpClient();

            while (true)
            {
                var response = await client.RequestTokenAsync(new TokenRequest
                {
                    Address = disco.TokenEndpoint,
                    ClientId = "ciba",
                    ClientSecret = "secret",
                    GrantType = "urn:openid:params:grant-type:ciba",
                    Parameters = 
                    {
                        { "auth_req_id", authorizeResponse.auth_req_id }
                    }
                });

                if (response.IsError)
                {
                    if (response.Error == OidcConstants.TokenErrors.AuthorizationPending || response.Error == OidcConstants.TokenErrors.SlowDown)
                    {
                        Console.WriteLine($"{response.Error}...waiting.");
                        Thread.Sleep(authorizeResponse.interval * 1000);
                    }
                    else
                    {
                        throw new Exception(response.Error);
                    }
                }
                else
                {
                    return response;
                }
            }
        }

        static async Task CallServiceAsync(string token)
        {
            var baseAddress = Constants.SampleApi;

            var client = new HttpClient
            {
                BaseAddress = new Uri(baseAddress)
            };

            client.SetBearerToken(token);
            var response = await client.GetStringAsync("identity");

            "\n\nService claims:".ConsoleGreen();
            Console.WriteLine(response.PrettyPrintJson());
        }
    }

    public class BackchannelLoginResponse
    {
        /// <summary>
        /// Indicates if this response represents an error.
        /// </summary>
        public bool IsError => !String.IsNullOrWhiteSpace(error);

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        public string error { get; set; }

        /// <summary>
        /// Gets or sets the error description.
        /// </summary>
        public string error_description { get; set; }

        /// <summary>
        /// Gets or sets the authentication request id.
        /// </summary>
        public string auth_req_id { get; set; }

        /// <summary>
        /// Gets or sets the expires in.
        /// </summary>
        public int expires_in { get; set; }

        /// <summary>
        /// Gets or sets the interval.
        /// </summary>
        public int interval { get; set; }
    }
}
