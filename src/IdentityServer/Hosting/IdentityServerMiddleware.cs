// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using Microsoft.Extensions.DependencyInjection;
using Duende.IdentityServer.Stores;
using System.Collections.Generic;

namespace Duende.IdentityServer.Hosting;

/// <summary>
/// IdentityServer middleware
/// </summary>
public class IdentityServerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityServerMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next.</param>
    /// <param name="logger">The logger.</param>
    public IdentityServerMiddleware(RequestDelegate next, ILogger<IdentityServerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="router">The router.</param>
    /// <param name="session">The user session.</param>
    /// <param name="events">The event service.</param>
    /// <param name="issuerNameService">The issuer name service</param>
    /// <param name="backChannelLogoutService"></param>
    /// <returns></returns>
    public async Task Invoke(
        HttpContext context, 
        IEndpointRouter router, 
        IUserSession session, 
        IEventService events,
        IIssuerNameService issuerNameService,
        IBackChannelLogoutService backChannelLogoutService)
    {
        // this will check the authentication session and from it emit the check session
        // cookie needed from JS-based signout clients.
        await session.EnsureSessionIdCookieAsync();

        context.Response.OnStarting(async () =>
        {
            if (context.GetSignOutCalled())
            {
                _logger.LogDebug("SignOutCalled set; processing post-signout session cleanup.");

                // this clears our session id cookie so JS clients can detect the user has signed out
                await session.RemoveSessionIdCookieAsync();

                await RevokeTokensAsync(context);

                // back channel logout
                var logoutContext = await session.GetLogoutNotificationContext();
                if (logoutContext != null)
                {
                    await backChannelLogoutService.SendLogoutNotificationsAsync(logoutContext);
                }
            }
        });

        try
        {
            var endpoint = router.Find(context);
            if (endpoint != null)
            {
                var endpointType = endpoint.GetType().FullName;
                
                using var activity = Tracing.ActivitySource.StartActivity("IdentityServerProtocolRequest");
                activity?.SetTag(Tracing.Properties.EndpointType, endpointType);
                
                LicenseValidator.ValidateIssuer(await issuerNameService.GetCurrentAsync());

                _logger.LogInformation("Invoking IdentityServer endpoint: {endpointType} for {url}", endpointType, context.Request.Path.ToString());

                var result = await endpoint.ProcessAsync(context);

                if (result != null)
                {
                    _logger.LogTrace("Invoking result: {type}", result.GetType().FullName);
                    await result.ExecuteAsync(context);
                }

                return;
            }
        }
        catch (Exception ex)
        {
            await events.RaiseAsync(new UnhandledExceptionEvent(ex));
            _logger.LogCritical(ex, "Unhandled exception: {exception}", ex.Message);
            throw;
        }

        await _next(context);
    }

    static readonly string[] OnlyTokenTypes = new[] {
        IdentityServerConstants.PersistedGrantTypes.RefreshToken,
        IdentityServerConstants.PersistedGrantTypes.ReferenceToken,
        IdentityServerConstants.PersistedGrantTypes.AuthorizationCode,
        IdentityServerConstants.PersistedGrantTypes.BackChannelAuthenticationRequest,
    };
    
    private static async Task RevokeTokensAsync(HttpContext context)
    {
        var session = context.RequestServices.GetRequiredService<IUserSession>();

        var user = await session.GetUserAsync();
        if (user != null)
        {
            var clientStore = context.RequestServices.GetRequiredService<IClientStore>();
            var persistedGrantStore = context.RequestServices.GetRequiredService<IPersistedGrantStore>();

            var sub = user.GetSubjectId();
            var sid = await session.GetSessionIdAsync();
            var clientIds = await session.GetClientListAsync();

            var clients = new List<string>();
            foreach (var clientId in clientIds)
            {
                var client = await clientStore.FindEnabledClientByIdAsync(clientId);
                if (client?.RevokeTokensAtUserLogout == true)
                {
                    clients.Add(clientId);
                }
            }

            if (clients.Count > 0)
            {
                await persistedGrantStore.RemoveAllAsync(new PersistedGrantFilter
                {
                    SubjectId = sub,
                    SessionId = sid,
                    ClientIds = clients,
                    Types = OnlyTokenTypes
                });
            }
        }
    }
}