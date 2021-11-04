// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Services
{
    /// <summary>
    /// Nop implementation of IUserLoginService.
    /// </summary>
    public class NopBackchannelAuthenticationUserNotificationService : IBackchannelAuthenticationUserNotificationService
    {
        private readonly ILogger<NopBackchannelAuthenticationUserNotificationService> _logger;

        /// <summary>
        /// Ctor
        /// </summary>
        public NopBackchannelAuthenticationUserNotificationService(ILogger<NopBackchannelAuthenticationUserNotificationService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public Task SendLoginRequestAsync(BackchannelUserLoginRequest request)
        {
            _logger.LogWarning("IUserLoginService not implemented.");
            return Task.CompletedTask;
        }
    }
}
