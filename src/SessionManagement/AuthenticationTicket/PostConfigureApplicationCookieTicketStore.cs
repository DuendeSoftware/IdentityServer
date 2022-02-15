// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Duende.SessionManagement;

/// <summary>
/// Cookie configuration for the user session plumbing
/// </summary>
public class PostConfigureApplicationCookieTicketStore : IPostConfigureOptions<CookieAuthenticationOptions>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string? _scheme;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    /// <param name="identityServerOptions"></param>
    /// <param name="options"></param>
    public PostConfigureApplicationCookieTicketStore(IHttpContextAccessor httpContextAccessor, IdentityServerOptions identityServerOptions, IOptions<Microsoft.AspNetCore.Authentication.AuthenticationOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;

        _scheme = identityServerOptions.Authentication.CookieAuthenticationScheme ??
             options.Value.DefaultAuthenticateScheme ??
             options.Value.DefaultScheme;
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    /// <param name="scheme"></param>
    public PostConfigureApplicationCookieTicketStore(IHttpContextAccessor httpContextAccessor, string scheme)
    {
        _httpContextAccessor = httpContextAccessor;
        _scheme = scheme;
    }

    /// <inheritdoc />
    public void PostConfigure(string name, CookieAuthenticationOptions options)
    {
        if (name == _scheme)
        {
            var sessionStore = _httpContextAccessor.HttpContext!.RequestServices.GetService<IUserSessionStore>();
            if (sessionStore is InMemoryUserSessionStore)
            {
                var logger = _httpContextAccessor.HttpContext!.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Duende.IdentityServer.Startup");
                logger.LogInformation("You are using the in-memory version of the user session store. This will store user authentication sessions server side, but in memory only. If you are using this feature in production, you want to switch to a different store implementation.");
            }

            options.SessionStore = new TicketStoreShim(_httpContextAccessor);
        }
    }
}
