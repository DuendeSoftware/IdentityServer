// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

using Duende.IdentityServer.Models;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Services;

/// <summary>
/// Interface for sending a user a login request from a backchannel authentication request.
/// </summary>
public interface IBackchannelAuthenticationUserNotificationService
{
    /// <summary>
    /// Sends a notification for the user to login.
    /// </summary>
    Task SendLoginRequestAsync(BackchannelUserLoginRequest request);
}
