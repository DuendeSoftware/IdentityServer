// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Duende.IdentityServer.Hosting.DynamicProviders;
using Duende.IdentityServer.IntegrationTests.TestFramework;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace IdentityServer.IntegrationTests.Hosting
{
    public class DynamicProvidersTests
    {
        private GenericHost _host;
        private GenericHost _idp1;

        List<OidcProvider> _oidcProviders = new List<OidcProvider>()
        { 
            new OidcProvider
            {
                Scheme = "test",
                Authority = "https://idp1",
                ClientId = "client",
                ClientSecret = "secret",
                ResponseType = "code",
            }
        };

        public string Idp1FrontChannelLogoutUri { get; set; }

        public DynamicProvidersTests()
        {
            _idp1 = new GenericHost("https://idp1");
            _idp1.OnConfigureServices += services =>
            {
                services.AddRouting();
                services.AddAuthorization();

                services.AddIdentityServer()
                    .AddInMemoryClients(new Client[] {
                        new Client
                        { 
                            ClientId = "client",
                            ClientSecrets = { new Secret("secret".Sha256()) },
                            AllowedGrantTypes = GrantTypes.Code,
                            RedirectUris = { "https://server/federation/test/signin" },
                            PostLogoutRedirectUris = { "https://server/federation/test/signout-callback" },
                            FrontChannelLogoutUri = "https://server/federation/test/signout",
                            AllowedScopes = { "openid" }
                        }
                    })
                    .AddInMemoryIdentityResources(new IdentityResource[] {
                        new IdentityResources.OpenId(),
                    })
                    .AddDeveloperSigningCredential(persistKey: false);
            };
            _idp1.OnConfigure += app =>
            {
                app.UseRouting();

                app.UseIdentityServer();
                app.UseAuthorization();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/signin", async ctx =>
                    {
                        await ctx.SignInAsync(new IdentityServerUser("1").CreatePrincipal());
                    });
                    endpoints.MapGet("/account/logout", async ctx =>
                    {
                        var isis = ctx.RequestServices.GetRequiredService<IIdentityServerInteractionService>();
                        var logoutCtx = await isis.GetLogoutContextAsync(ctx.Request.Query["logoutId"]);
                        Idp1FrontChannelLogoutUri = logoutCtx.SignOutIFrameUrl;
                        await ctx.SignOutAsync();
                    });
                });
            };
            _idp1.InitializeAsync().Wait();



            _host = new GenericHost("https://server");
            _host.OnConfigureServices += services => 
            {
                services.AddRouting();
                services.AddAuthorization();

                services.AddIdentityServer()
                    .AddInMemoryClients(new Client[] { })
                    .AddInMemoryIdentityResources(new IdentityResource[] { })
                    .AddInMemoryOidcProviders(_oidcProviders)
                    .AddIdentityProviderStoreCache()
                    .AddDeveloperSigningCredential(persistKey: false);

                services.ConfigureAll<OpenIdConnectOptions>(options =>
                {
                    options.BackchannelHttpHandler = _idp1.Server.CreateHandler();
                });
            };
            _host.OnConfigure += app => 
            {
                app.UseRouting();
                
                app.UseIdentityServer();
                app.UseAuthorization();
                
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/", async ctx =>
                    {
                        var session = await ctx.AuthenticateAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);
                        if (session.Succeeded)
                        {
                            await ctx.Response.WriteAsync(session.Principal.FindFirst("sub").Value);
                        }
                        else
                        {
                            ctx.Response.StatusCode = 401;
                        }
                    });
                    endpoints.MapGet("/challenge", async ctx =>
                    {
                        await ctx.ChallengeAsync(ctx.Request.Query["scheme"]);
                    });
                });
            };

            _host.InitializeAsync().Wait();
        }

        [Fact]
        public async Task challenge_should_trigger_authorize_request_to_idp()
        {
            var response = await _host.HttpClient.GetAsync(_host.Url("/challenge?scheme=test"));

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location.ToString().Should().StartWith("https://idp1/connect/authorize");
        }

#if NET5_0_OR_GREATER
        // the cookie processing in this workflow requires updates to .NET5 for our test browser and cookie container
        // https://github.com/dotnet/runtime/issues/26776
        
        [Fact]
        public async Task redirect_uri_should_process_signin_result()
        {
            var response = await _host.BrowserClient.GetAsync(_host.Url("/challenge?scheme=test"));
            var authzUrl = response.Headers.Location.ToString();

            await _idp1.BrowserClient.GetAsync(_idp1.Url("/signin"));
            response = await _idp1.BrowserClient.GetAsync(authzUrl);
            var redirectUri = response.Headers.Location.ToString();
            redirectUri.Should().StartWith("https://server/federation/test/signin");

            await _host.BrowserClient.GetAsync(redirectUri);

            response = await _host.BrowserClient.GetAsync(_host.Url("/"));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Be("1"); // sub
        }


        [Fact]
        public async Task redirect_uri_should_work_when_dynamic_provider_not_in_cache()
        {
            var response = await _host.BrowserClient.GetAsync(_host.Url("/challenge?scheme=test"));
            var authzUrl = response.Headers.Location.ToString();

            await _idp1.BrowserClient.GetAsync(_idp1.Url("/signin"));
            response = await _idp1.BrowserClient.GetAsync(authzUrl);
            var redirectUri = response.Headers.Location.ToString();
            redirectUri.Should().StartWith("https://server/federation/test/signin");

            var cache = _host.Resolve<IdentityProviderCache>();
            cache.Remove("test");

            await _host.BrowserClient.GetAsync(redirectUri);

            response = await _host.BrowserClient.GetAsync(_host.Url("/"));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Be("1"); // sub
        }
#endif
    }
}
