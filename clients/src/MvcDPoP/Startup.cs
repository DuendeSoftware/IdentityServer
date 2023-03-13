using Clients;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace MvcDPoP
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

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
                    options.Cookie.Name = "mvcdpop";

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

                    options.ClientId = "mvc.dpop";
                    options.ClientSecret = "secret";

                    // code flow + PKCE (PKCE is turned on by default)
                    options.ResponseType = "code";
                    options.ResponseMode = "query";
                    options.UsePkce = true;

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("resource1.scope1");
                    options.Scope.Add("offline_access");

                    // keeps id_token smaller
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };

                    options.BackchannelHttpHandler = new DPoPProofTokenEndpointMessageHandler();
                    options.EventsType = typeof(DPoPOpenIdConnectEvents);
                });

            services.AddTransient<DPoPOpenIdConnectEvents>();

            // add automatic token management
            services.AddOpenIdConnectAccessTokenManagement(options => { 
                // options.UseDPoP = true;
            })
                // .AddDPoPKey(...)
                // or
                // .AddSessionDPopKey() // => assumes something more about where key is
                // or
                // .AddDPoPKeyStore<T>()
            ;

            // add HTTP client to call protected API
            services.AddUserAccessTokenHttpClient("client", configureClient: client =>
            {
                client.BaseAddress = new Uri(Constants.SampleApi);
                // somehow allow this HttpClient to override the scheme (because it might be a legacy API still using Bearer)
            }).AddHttpMessageHandler<DPoPProofApiMessageHandler>();
            
            services.AddTransient<DPoPProofApiMessageHandler>();

            // decorator
            services.AddTransient<UserTokenEndpointService>();
            services.AddTransient<IUserTokenEndpointService, DPoPUserTokenEndpointService>();
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