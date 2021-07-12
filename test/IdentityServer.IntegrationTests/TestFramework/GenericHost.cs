// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Duende.IdentityServer.IntegrationTests.TestFramework
{
    public class GenericHost
    {
        public GenericHost(string baseAddress = "https://server")
        {
            if (baseAddress.EndsWith("/")) baseAddress = baseAddress.Substring(0, baseAddress.Length - 1);
            _baseAddress = baseAddress;
        }

        private readonly string _baseAddress;
        IServiceProvider _appServices;

        public Assembly HostAssembly { get; set; }
        public bool IsDevelopment { get; set; }

        public TestServer Server { get; private set; }
        public TestBrowserClient BrowserClient { get; set; }
        public HttpClient HttpClient { get; set; }

        public TestLoggerProvider Logger { get; set; } = new TestLoggerProvider();


        public T Resolve<T>()
        {
            // not calling dispose on scope on purpose
            return _appServices.GetRequiredService<IServiceScopeFactory>().CreateScope().ServiceProvider.GetRequiredService<T>();
        }

        public string Url(string path = null)
        {
            path = path ?? String.Empty;
            if (!path.StartsWith("/")) path = "/" + path;
            return _baseAddress + path;
        }

        public async Task InitializeAsync()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(builder =>
                {
                    builder.UseTestServer();

                    builder.ConfigureAppConfiguration((context, b) =>
                    {
                        if (HostAssembly != null)
                        {
                            context.HostingEnvironment.ApplicationName = HostAssembly.GetName().Name;
                        }
                    });

                    if (IsDevelopment)
                    {
                        builder.UseSetting("Environment", "Development");
                    }
                    else
                    {
                        builder.UseSetting("Environment", "Production");
                    }

                    builder.ConfigureServices(ConfigureServices);
                    builder.Configure(ConfigureApp);
                });

            // Build and start the IHost
            var host = await hostBuilder.StartAsync();

            Server = host.GetTestServer();
            BrowserClient = new TestBrowserClient(Server.CreateHandler());
            HttpClient = Server.CreateClient();
        }

        public event Action<IServiceCollection> OnConfigureServices = services => { };
        public event Action<IApplicationBuilder> OnConfigure = app => { };

        void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(options =>
            {
                options.SetMinimumLevel(LogLevel.Warning);
                options.AddProvider(Logger);
            });

            OnConfigureServices(services);
        }

        void ConfigureApp(IApplicationBuilder app)
        {
            _appServices = app.ApplicationServices;
            
            OnConfigure(app);

            ConfigureSignin(app);
            ConfigureSignout(app);
        }



        void ConfigureSignout(IApplicationBuilder app)
        {
            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path == "/__signout")
                {
                    await ctx.SignOutAsync();
                    ctx.Response.StatusCode = 204;
                    return;
                }

                await next();
            });
        }
        public async Task RevokeSessionCookieAsync()
        {
            var response = await BrowserClient.GetAsync(Url("__signout"));
            response.StatusCode.Should().Be(204);
        }


        void ConfigureSignin(IApplicationBuilder app)
        {
            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path == "/__signin")
                {
                    if (_userToSignIn != null)
                    {
                        throw new Exception("No User Configured for SignIn");
                    }

                    var props = _propsToSignIn ?? new AuthenticationProperties();
                    await ctx.SignInAsync(_userToSignIn, props);
                    
                    _userToSignIn = null;
                    _propsToSignIn = null;

                    ctx.Response.StatusCode = 204;
                    return;
                }

                await next();
            });
        }
        ClaimsPrincipal _userToSignIn;
        AuthenticationProperties _propsToSignIn;
        public async Task IssueSessionCookieAsync(params Claim[] claims)
        {
            _userToSignIn = new ClaimsPrincipal(new ClaimsIdentity(claims, "test", "name", "role"));
            var response = await BrowserClient.GetAsync(Url("__signin"));
            response.StatusCode.Should().Be(204);
        }
        public Task IssueSessionCookieAsync(AuthenticationProperties props, params Claim[] claims)
        {
            _propsToSignIn = props;
            return IssueSessionCookieAsync(claims);
        }
        public Task IssueSessionCookieAsync(string sub, params Claim[] claims)
        {
            return IssueSessionCookieAsync(claims.Append(new Claim("sub", sub)).ToArray());
        }
    }
}
