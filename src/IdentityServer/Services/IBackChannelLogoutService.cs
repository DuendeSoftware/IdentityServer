// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

using System.Threading.Tasks;
using Duende.IdentityServer.Models;

namespace Duende.IdentityServer.Services;

/// <summary>
/// The service responsible for performing back-channel logout notification.
/// </summary>
public interface IBackChannelLogoutService
{
    /// <summary>
    /// Performs http back-channel logout notification.
    /// </summary>
    /// <param name="context">The context of the back channel logout notification.</param>
    Task SendLogoutNotificationsAsync(LogoutNotificationContext context);
}
