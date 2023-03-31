// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Linq;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Authentication;
using static Duende.IdentityServer.IdentityServerConstants;

#pragma warning disable 1591

namespace Duende.IdentityServer.Extensions;

public static class HttpContextExtensions
{
    [Obsolete("For a replacement, use IAuthenticationHandlerProvider.GetHandlerAsync and check if the handler implements IAuthenticationSignOutHandler.")]
    public static async Task<bool> GetSchemeSupportsSignOutAsync(this HttpContext context, string scheme)
    {
        var provider = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
        var handler = await provider.GetHandlerAsync(context, scheme);
        return (handler is IAuthenticationSignOutHandler);
    }

    [Obsolete("Use IServerUrls.Origin instead.")]
    public static void SetIdentityServerOrigin(this HttpContext context, string value)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        context.RequestServices.GetRequiredService<IServerUrls>().Origin = value;
    }

    [Obsolete("Use IServerUrls.BasePath instead.")]
    public static void SetIdentityServerBasePath(this HttpContext context, string value)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        context.RequestServices.GetRequiredService<IServerUrls>().BasePath = value;
    }

    [Obsolete("Use IIssuerNameService instead.")]
    public static string GetIdentityServerOrigin(this HttpContext context)
    {
        var options = context.RequestServices.GetRequiredService<IdentityServerOptions>();
        var request = context.Request;
            
        if (options.MutualTls.Enabled && options.MutualTls.DomainName.IsPresent())
        {
            if (!options.MutualTls.DomainName.Contains("."))
            {
                if (request.Host.Value.StartsWith(options.MutualTls.DomainName, StringComparison.OrdinalIgnoreCase))
                {
                    return request.Scheme + "://" +
                           request.Host.Value.Substring(options.MutualTls.DomainName.Length + 1);
                }
            }
        }
            
        return request.Scheme + "://" + request.Host.Value;
    }


    internal static void SetSignOutCalled(this HttpContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        context.Items[Constants.EnvironmentKeys.SignOutCalled] = "true";
    }

    internal static bool GetSignOutCalled(this HttpContext context)
    {
        return context.Items.ContainsKey(Constants.EnvironmentKeys.SignOutCalled);
    }

    /// <summary>
    /// Gets the host name of IdentityServer.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    [Obsolete("Use IServerUrls.Origin instead.")]
    public static string GetIdentityServerHost(this HttpContext context)
    {
        return context.RequestServices.GetRequiredService<IServerUrls>().Origin;
    }

    /// <summary>
    /// Gets the base path of IdentityServer.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    [Obsolete("Use IServerUrls.BasePath instead.")]
    public static string GetIdentityServerBasePath(this HttpContext context)
    {
        return context.RequestServices.GetRequiredService<IServerUrls>().BasePath;
    }

    /// <summary>
    /// Gets the public base URL for IdentityServer.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    [Obsolete("Use IServerUrls.BaseUrl instead.")]
    public static string GetIdentityServerBaseUrl(this HttpContext context)
    {
        return context.RequestServices.GetRequiredService<IServerUrls>().BaseUrl;
    }

    /// <summary>
    /// Gets the identity server relative URL.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    [Obsolete("Use IServerUrls.GetIdentityServerRelativeUrl instead.")]
    public static string GetIdentityServerRelativeUrl(this HttpContext context, string path)
    {
        return context.RequestServices.GetRequiredService<IServerUrls>().GetIdentityServerRelativeUrl(path);
    }

    /// <summary>
    /// Gets the identity server issuer URI.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException">context</exception>
    [Obsolete("Use the IIssuerNameService instead.")]
    public static string GetIdentityServerIssuerUri(this HttpContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        return context.RequestServices.GetRequiredService<IIssuerNameService>().GetCurrentAsync().GetAwaiter().GetResult();
    }

    internal static async Task<string> GetIdentityServerSignoutFrameCallbackUrlAsync(this HttpContext context, LogoutMessage logoutMessage = null)
    {
        var userSession = context.RequestServices.GetRequiredService<IUserSession>();
        var user = await userSession.GetUserAsync();
        var currentSubId = user?.GetSubjectId();

        LogoutNotificationContext endSessionMsg = null;

        // if we have a logout message, then that take precedence over the current user
        if (logoutMessage?.ClientIds?.Any() == true)
        {
            var clientIds = logoutMessage?.ClientIds;

            // check if current user is same, since we might have new clients (albeit unlikely)
            if (currentSubId == logoutMessage?.SubjectId)
            {
                clientIds = clientIds.Union(await userSession.GetClientListAsync());
                clientIds = clientIds.Distinct();
            }

            endSessionMsg = new LogoutNotificationContext
            {
                SubjectId = logoutMessage.SubjectId,
                SessionId = logoutMessage.SessionId,
                ClientIds = clientIds
            };
        }
        else if (currentSubId != null)
        {
            // see if current user has any clients they need to signout of 
            var clientIds = await userSession.GetClientListAsync();
            if (clientIds.Any())
            {
                endSessionMsg = new LogoutNotificationContext
                {
                    SubjectId = currentSubId,
                    SessionId = await userSession.GetSessionIdAsync(),
                    ClientIds = clientIds
                };
            }
        }

        if (endSessionMsg != null)
        {
            var clock = context.RequestServices.GetRequiredService<ISystemClock>();
            var msg = new Message<LogoutNotificationContext>(endSessionMsg, clock.UtcNow.UtcDateTime);

            var endSessionMessageStore = context.RequestServices.GetRequiredService<IMessageStore<LogoutNotificationContext>>();
            var id = await endSessionMessageStore.WriteAsync(msg);

            var urls = context.RequestServices.GetRequiredService<IServerUrls>();
            var signoutIframeUrl = urls.BaseUrl.EnsureTrailingSlash() + ProtocolRoutePaths.EndSessionCallback;
            signoutIframeUrl = signoutIframeUrl.AddQueryString(Constants.UIConstants.DefaultRoutePathParams.EndSessionCallback, id);

            return signoutIframeUrl;
        }

        // no sessions, so nothing to cleanup
        return null;
    }
}