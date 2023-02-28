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
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IntegrationTests.TestFramework;

public class GenericHost
{
    public GenericHost(string baseAddress = "https://server")
    {
        if (baseAddress.EndsWith("/")) baseAddress = baseAddress.Substring(0, baseAddress.Length - 1);
        _baseAddress = baseAddress;
    }

    private readonly string _baseAddress;
    protected IServiceProvider? _appServices;

    public Assembly? HostAssembly { get; set; }
    public bool IsDevelopment { get; set; }

    public TestServer? Server { get; private set; }
    // public TestBrowserClient? BrowserClient { get; set; }
    public HttpClient? HttpClient { get; set; }

    public TestLoggerProvider Logger { get; set; } = new TestLoggerProvider();


    public T Resolve<T>()
        where T : notnull
    {
        if(_appServices == null) 
        {
            throw new Exception("Attempt to resolve services before service provider created. Call ConfigureApp first");
        }
        // not calling dispose on scope on purpose
        return _appServices.GetRequiredService<IServiceScopeFactory>().CreateScope().ServiceProvider.GetRequiredService<T>();
    }

    public string Url(string path = "")
    {
        path = path ?? String.Empty;
        if (!path.StartsWith("/")) path = "/" + path;
        return _baseAddress + path;
    }

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = IsDevelopment ? "Development" : "Production"
        });
        builder.WebHost.UseTestServer();

        if (HostAssembly is not null)
        {
            builder.Environment.ApplicationName = HostAssembly.GetName().Name ?? "";
        }

        ConfigureServices(builder.Services);
        var app = builder.Build();
        ConfigureApp(app);
        
        // Build and start the IHost
        await app.StartAsync();

        Server = app.GetTestServer();
        // BrowserClient = new TestBrowserClient(Server.CreateHandler());
        HttpClient = Server.CreateClient();
    }

    public event Action<IServiceCollection> OnConfigureServices = services => { };
    public event Action<WebApplication> OnConfigure = app => { };

    void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(options =>
        {
            options.SetMinimumLevel(LogLevel.Debug);
            options.AddProvider(Logger);
        });

        OnConfigureServices(services);
    }

    void ConfigureApp(WebApplication app)
    {
        _appServices = app.Services;
        
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
    // public async Task RevokeSessionCookieAsync()
    // {
    //     if(BrowserClient is null) 
    //     {
    //         throw new Exception("Attempt to use BrowserClient before it is initialized. Call InitializeAsync.");
    //     }
    //     var response = await BrowserClient.GetAsync(Url("__signout"));
    //     response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    // }


    void ConfigureSignin(IApplicationBuilder app)
    {
        app.Use(async (ctx, next) =>
        {
            if (ctx.Request.Path == "/__signin")
            {
                if (_userToSignIn is not object)
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
    ClaimsPrincipal? _userToSignIn;
    AuthenticationProperties? _propsToSignIn;
    // public async Task IssueSessionCookieAsync(params Claim[] claims)
    // {
    //     if(BrowserClient is null) 
    //     {
    //         throw new Exception("Attempt to use BrowserClient before it is initialized. Call InitializeAsync.");
    //     }
    //     _userToSignIn = new ClaimsPrincipal(new ClaimsIdentity(claims, "test", "name", "role"));
    //     var response = await BrowserClient.GetAsync(Url("__signin"));
    //     response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    // }
    // public Task IssueSessionCookieAsync(AuthenticationProperties props, params Claim[] claims)
    // {
    //     _propsToSignIn = props;
    //     return IssueSessionCookieAsync(claims);
    // }
    // public Task IssueSessionCookieAsync(string sub, params Claim[] claims)
    // {
    //     return IssueSessionCookieAsync(claims.Append(new Claim("sub", sub)).ToArray());
    // }
}
