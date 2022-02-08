// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
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

    /// <inheritdoc />
    public void PostConfigure(string name, CookieAuthenticationOptions options)
    {
        if (name == _scheme)
        {
            options.SessionStore = new TicketStoreShim(_httpContextAccessor);
        }
    }
}
