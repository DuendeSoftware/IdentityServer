// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using System.Threading.Tasks;

namespace IdentityServer.IntegrationTests.Endpoints.Ciba
{
    internal class MockCibaUserNotificationService : IBackchannelAuthenticationUserNotificationService
    {
        public BackchannelUserLoginRequest LoginRequest { get; set; }

        public Task SendLoginRequestAsync(BackchannelUserLoginRequest request)
        {
            LoginRequest = request;
            return Task.CompletedTask;
        }
    }
}
