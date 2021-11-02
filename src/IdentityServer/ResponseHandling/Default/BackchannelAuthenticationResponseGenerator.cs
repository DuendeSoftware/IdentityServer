// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Validation;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Duende.IdentityServer.Stores;

namespace Duende.IdentityServer.ResponseHandling
{
    /// <summary>
    /// The backchannel authentication response generator
    /// </summary>
    /// <seealso cref="IBackchannelAuthenticationResponseGenerator" />
    public class BackchannelAuthenticationResponseGenerator : IBackchannelAuthenticationResponseGenerator
    {
        /// <summary>
        /// The options
        /// </summary>
        protected readonly IdentityServerOptions Options;
        
        /// <summary>
        /// The request store.
        /// </summary>
        protected readonly IBackChannelAuthenticationRequestStore BackChannelAuthenticationRequestStore;

        /// <summary>
        /// The user login service.
        /// </summary>
        protected readonly IUserLoginService UserLoginService;

        /// <summary>
        /// The clock
        /// </summary>
        protected readonly ISystemClock Clock;

        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackchannelAuthenticationResponseGenerator"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="backChannelAuthenticationRequestStore"></param>
        /// <param name="userLoginService"></param>
        /// <param name="clock">The clock.</param>
        /// <param name="logger">The logger.</param>
        public BackchannelAuthenticationResponseGenerator(IdentityServerOptions options,
            IBackChannelAuthenticationRequestStore backChannelAuthenticationRequestStore,
            IUserLoginService userLoginService,
            ISystemClock clock, 
            ILogger<BackchannelAuthenticationResponseGenerator> logger)
        {
            Options = options;
            BackChannelAuthenticationRequestStore = backChannelAuthenticationRequestStore;
            UserLoginService = userLoginService;
            Clock = clock;
            Logger = logger;
        }

        /// <inheritdoc/>
        public virtual async Task<BackchannelAuthenticationResponse> ProcessAsync(BackchannelAuthenticationRequestValidationResult validationResult)
        {
            if (validationResult == null) throw new ArgumentNullException(nameof(validationResult));
            if (validationResult.ValidatedRequest == null) throw new ArgumentNullException(nameof(validationResult.ValidatedRequest));
            if (validationResult.ValidatedRequest.Client == null) throw new ArgumentNullException(nameof(validationResult.ValidatedRequest.Client));

            Logger.LogTrace("Creating response for backchannel authentication request");

            var request = new BackChannelAuthenticationRequest
            { 
                CreationTime = Clock.UtcNow.DateTime,
                ClientId = validationResult.ValidatedRequest.ClientId,
                RequestedScopes = validationResult.ValidatedRequest.ValidatedResources.RawScopeValues,
                RequestedResourceIndicators = validationResult.ValidatedRequest.RequestedResourceIndiators,
                Subject = validationResult.ValidatedRequest.Subject,
                Lifetime = validationResult.ValidatedRequest.Expiry,
                AuthenticationContextReferenceClasses = validationResult.ValidatedRequest.AuthenticationContextReferenceClasses,
                BindingMessage = validationResult.ValidatedRequest.BindingMessage,
            };

            var requestId = await BackChannelAuthenticationRequestStore.CreateRequestAsync(request);

            var response = new BackchannelAuthenticationResponse()
            {
                AuthenticationRequestId = requestId,
                ExpiresIn = request.Lifetime,
                Interval = Options.Ciba.PollingInterval,
            };

            await UserLoginService.SendRequestAsync(new UserLoginRequest
            {
                RequestId = requestId,
                Subject = validationResult.ValidatedRequest.Subject,
                BindingMessage = validationResult.ValidatedRequest.BindingMessage,
                AuthenticationContextReferenceClasses = validationResult.ValidatedRequest.AuthenticationContextReferenceClasses,
            });

            return response;
        }
    }
}
