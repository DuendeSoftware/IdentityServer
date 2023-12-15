using Clients;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using Microsoft.Extensions.Configuration;
using Duende.AccessTokenManagement;

namespace MvcJarAndPar
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<AssertionService>();
            services.AddTransient<OidcEvents>();

            // add MVC
            services.AddControllersWithViews();

            // add cookie-based session management with OpenID Connect authentication
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "cookie";
                options.DefaultChallengeScheme = "oidc";
            })
                .AddCookie("cookie", options =>
                {
                    options.Cookie.Name = "mvc.jar.par";

                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = false;

                    options.Events.OnSigningOut = async e =>
                    {
                        // automatically revoke refresh token at signout time
                        await e.HttpContext.RevokeRefreshTokenAsync();
                    };
                })
                .AddOpenIdConnect("oidc", options =>
                {
                    options.Authority = Constants.Authority;
                    options.RequireHttpsMetadata = false;

                    options.ClientId = "mvc.jar.par";
                    // options.ClientSecret = "secret";

                    // code flow + PKCE (PKCE is turned on by default)
                    options.ResponseType = "code";
                    options.UsePkce = true;

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("resource1.scope1");
                    options.Scope.Add("offline_access");

                    // keeps id_token smaller
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;
                    options.MapInboundClaims = false;
                    
                    // needed to add JWR / private_key_jwt support
                    options.EventsType = typeof(OidcEvents);
                    
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };

                    options.DisableTelemetry = true;
                });

            // add automatic token management
            services.AddOpenIdConnectAccessTokenManagement();

            // add HTTP client to call protected API
            services.AddUserAccessTokenHttpClient("client", configureClient: client =>
            {
                client.BaseAddress = new Uri(Constants.SampleApi);
            });
            
            // var apiKey = _configuration["HoneyCombApiKey"];
            // var dataset = "IdentityServerDev";
            //
            // services.AddOpenTelemetryTracing(builder =>
            // {
            //     builder
            //         //.AddConsoleExporter()
            //         .SetResourceBuilder(
            //             ResourceBuilder.CreateDefault()
            //                 .AddService("MVC JAR JWT"))
            //         //.SetSampler(new AlwaysOnSampler())
            //         .AddHttpClientInstrumentation()
            //         .AddAspNetCoreInstrumentation()
            //         .AddSqlClientInstrumentation()
            //         .AddOtlpExporter(option =>
            //         {
            //             option.Endpoint = new Uri("https://api.honeycomb.io");
            //             option.Headers = $"x-honeycomb-team={apiKey},x-honeycomb-dataset={dataset}";
            //         });
            // });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute()
                    .RequireAuthorization();
            });
        }
    }
}