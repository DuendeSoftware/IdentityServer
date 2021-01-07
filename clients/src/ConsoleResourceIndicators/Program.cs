using Clients;
using IdentityModel.OidcClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleResourceIndicators
{
    public class Program
    {
        static OidcClient _oidcClient;
        
        public static async Task Main()
        {
            Console.WriteLine("+------------------------------+");
            Console.WriteLine("|  Resource Indicators Demo    |");
            Console.WriteLine("+------------------------------+");
            Console.WriteLine("");

            while (true)
            {
                Console.WriteLine("\n\n");

                "a) scopes: resource1.scope1, resource2.scope1, resource3.scope1 -- no resource indicator".ConsoleGreen();
                "b) foo".ConsoleGreen();
                "c) foo".ConsoleGreen();
                
                "x) exit".ConsoleGreen();
                var key = Console.ReadKey();

                switch (key.Key)
                {
                    case ConsoleKey.A:
                        await FrontChannel("resource1.scope1 resource2.scope1 resource3.scope1", Array.Empty<string>());
                        break;
                    case ConsoleKey.X:
                        return;
                }
                
                
            }
        }

        private static async Task FrontChannel(string scope, IEnumerable<string> resource)
        {
            // create a redirect URI using an available port on the loopback address.
            // requires the OP to allow random ports on 127.0.0.1 - otherwise set a static port
            var browser = new SystemBrowser();
            string redirectUri = string.Format($"http://127.0.0.1:{browser.Port}");

            var options = new OidcClientOptions
            {
                Authority = Constants.Authority,

                ClientId = "console.resource.indicators",

                RedirectUri = redirectUri,
                Scope = scope,
                FilterClaims = false,
                LoadProfile = false,
                Browser = browser,
                
                Policy =
                {
                    RequireIdentityTokenSignature = false
                }
            };

            var serilog = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}")
                .CreateLogger();

            options.LoggerFactory.AddSerilog(serilog);

            _oidcClient = new OidcClient(options);
            var request = new LoginRequest
            {
                FrontChannel =
                {
                    Resource = resource.ToList()
                }
            };
            
            var result = await _oidcClient.LoginAsync(request);

            ShowResult(result);
        }

        private static void ShowResult(LoginResult result)
        {
            if (result.IsError)
            {
                Console.WriteLine("\n\nError:\n{0}", result.Error);
                return;
            }

            Console.WriteLine($"access token:   {result.AccessToken}");
            Console.WriteLine($"refresh token:  {result?.RefreshToken ?? "none"}");
        }
    }
}