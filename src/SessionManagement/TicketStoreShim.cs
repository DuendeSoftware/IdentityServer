// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Duende.SessionManagement;

/// <summary>
/// this shim class is needed since ITicketStore is not configured in DI, rather it's a property 
/// of the cookie options and coordinated with PostConfigureApplicationCookie. #lame
/// https://github.com/aspnet/AspNetCore/issues/6946 
/// </summary>
public class TicketStoreShim : ITicketStore
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    public TicketStoreShim(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// The inner
    /// </summary>
    private IServerSideTicketStore Inner => _httpContextAccessor.HttpContext!.RequestServices.GetRequiredService<IServerSideTicketStore>();

    /// <inheritdoc />
    public Task RemoveAsync(string key)
    {
        return Inner.RemoveAsync(key);
    }

    /// <inheritdoc />
    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        return Inner.RenewAsync(key, ticket);
    }

    /// <inheritdoc />
    public Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        return Inner.RetrieveAsync(key);
    }

    /// <inheritdoc />
    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        return Inner.StoreAsync(ticket);
    }
}
