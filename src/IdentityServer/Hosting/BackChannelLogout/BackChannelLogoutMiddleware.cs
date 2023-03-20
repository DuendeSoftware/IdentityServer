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
using Duende.IdentityServer.Models;
using System.Linq;
using Duende.IdentityServer.Configuration;

namespace Duende.IdentityServer.Hosting;

/// <summary>
/// Middleware that processes backchannel logout requests for upstream IdPs
/// </summary>
public class BackChannelLogoutMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityServerMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next.</param>
    /// <param name="logger">The logger.</param>
    public BackChannelLogoutMiddleware(RequestDelegate next, ILogger<BackChannelLogoutMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <returns></returns>
    public async Task Invoke(
        HttpContext context, 
        IdentityServerOptions options,
        IBackchannelLogoutRequestValidator backchannelLogoutRequestValidator)
    {
        // this is needed to dynamically determine the external scheme for the upstream IdP
        if (context.Request.Path.StartsWithSegments(options.BackchannelLogout.PathPrefix))
        {
            var startIndex = options.BackchannelLogout.PathPrefix.ToString().Length;
            if (context.Request.Path.Value.Length > startIndex)
            {
                var scheme = context.Request.Path.Value.Substring(startIndex + 1);
                var idx = scheme.IndexOf('/');
                if (idx > 0)
                {
                    // this assumes the path is: /<PathPrefix>/<scheme>
                    // e.g.: /backchannel/my-oidc-provider
                    scheme = scheme.Substring(0, idx);
                }

                var result = await backchannelLogoutRequestValidator.ValidateAsync(new BackchannelLogoutRequest
                {
                    Scheme = scheme,
                });
            }
        }
        
        await _next(context);
    }
}

    /*
 /// <inheritdoc />
    public virtual async Task ProcessRequestAsync(HttpContext context)
    {
        Logger.LogDebug("Processing back-channel logout request");
        
        context.Response.Headers.Add("Cache-Control", "no-cache, no-store");
        context.Response.Headers.Add("Pragma", "no-cache");

        try
        {
            if (context.Request.HasFormContentType)
            {
                var logoutToken = context.Request.Form[OidcConstants.BackChannelLogoutRequest.LogoutToken].FirstOrDefault();
                    
                if (!String.IsNullOrWhiteSpace(logoutToken))
                {
                    var user = await ValidateLogoutTokenAsync(logoutToken);
                    if (user != null)
                    {
                        // these are the sub & sid to signout
                        var sub = user.FindFirst("sub")?.Value;
                        var sid = user.FindFirst("sid")?.Value;
                            
                        Logger.BackChannelLogout(sub ?? "missing", sid ?? "missing");
                            
                        await UserSession.RevokeSessionsAsync(new UserSessionsFilter 
                        { 
                            SubjectId = sub,
                            SessionId = sid
                        });
                            
                        return;
                    }
                }
                else
                {
                    Logger.BackChannelLogoutError($"Failed to process backchannel logout request. 'Logout token is missing'");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.BackChannelLogoutError($"Failed to process backchannel logout request. '{ex.Message}'");
        }
            
        Logger.BackChannelLogoutError($"Failed to process backchannel logout request.");
        context.Response.StatusCode = 400;
    }

    

    
    */