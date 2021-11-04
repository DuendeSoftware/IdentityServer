// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Services
{
    /// <summary>
    /// Default implementation of IBackchannelAuthenticationInteractionService.
    /// </summary>
    public class DefaultBackchannelAuthenticationInteractionService : IBackchannelAuthenticationInteractionService
    {
        private readonly IBackChannelAuthenticationRequestStore _requestStore;
        private readonly IClientStore _clientStore;
        private readonly IUserSession _session;
        private readonly IResourceValidator _resourceValidator;
        private readonly ILogger<DefaultBackchannelAuthenticationInteractionService> _logger;

        /// <summary>
        /// Ctor
        /// </summary>
        public DefaultBackchannelAuthenticationInteractionService(
            IBackChannelAuthenticationRequestStore requestStore,
            IClientStore clients,
            IUserSession session,
            IResourceValidator resourceValidator,
            ILogger<DefaultBackchannelAuthenticationInteractionService> logger
)
        {
            _requestStore = requestStore;
            _clientStore = clients;
            _session = session;
            _resourceValidator = resourceValidator;
            _logger = logger;
        }

        async Task<BackchannelUserLoginRequest> CreateAsync(BackChannelAuthenticationRequest request)
        {
            if (request == null)
            {
                return null;
            }

            var client = await _clientStore.FindEnabledClientByIdAsync(request.ClientId);
            if (client == null)
            {
                return null;
            }

            var validatedResources = await _resourceValidator.ValidateRequestedResourcesAsync(new ResourceValidationRequest
            {
                Client = client,
                Scopes = request.RequestedScopes,
                ResourceIndicators = request.RequestedResourceIndicators,
            });

            return new BackchannelUserLoginRequest
            {
                Id = request.Id,
                Subject = request.Subject,
                Client = client,
                ValidatedResources = validatedResources,
                RequestedResourceIndicators = request.RequestedResourceIndicators,
                AuthenticationContextReferenceClasses = request.AuthenticationContextReferenceClasses,
                BindingMessage = request.BindingMessage,
            };
        }

        /// <inheritdoc/>
        public async Task<BackchannelUserLoginRequest> GetPendingLoginRequestById(string id)
        {
            var request = await _requestStore.GetByIdAsync(id);
            return await CreateAsync(request);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<BackchannelUserLoginRequest>> GetLoginRequestsForSubjectAsync(string sub)
        {
            var items = await _requestStore.GetLoginsForUserAsync(sub);
            var list = new List<BackchannelUserLoginRequest>();
            foreach(var item in items)
            {
                if (!item.IsAuthorized)
                {
                    var req = await CreateAsync(item);
                    if (req != null)
                    {
                        list.Add(req);
                    }
                }
            }
            return list;
        }

        /// <inheritdoc/>
        public Task RemoveLoginRequestAsync(string id)
        {
            return _requestStore.RemoveByIdAsync(id);
        }

        /// <inheritdoc/>
        public async Task HandleRequestByIdAsync(string id, ConsentResponse response)
        {
            if (String.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            var request = await _requestStore.GetByIdAsync(id);
            if (request == null)
            {
                _logger.LogError("Invalid backchannel authentication request id {id}", id);
                return;
            }

            var client = await _clientStore.FindEnabledClientByIdAsync(request.ClientId);
            if (client == null)
            {
                _logger.LogError("Invalid client {clientId}", request.ClientId);
                return;
            }

            var subject = await _session.GetUserAsync();
            if (subject == null)
            {
                _logger.LogError("No user present");
                return;
            }

            if (subject.GetSubjectId() != request.Subject.GetSubjectId())
            {
                _logger.LogError("Current user's subject id: {currentSubjectId} does not match subject id for backchannel authentication request: {storedSubjectId}", subject.GetSubjectId(), request.Subject.GetSubjectId());
                return;
            }

            var sid = await _session.GetSessionIdAsync();

            request.IsAuthorized = true;
            request.Subject = subject;
            request.AuthorizedScopes = response.ScopesValuesConsented;
            request.SessionId = sid;
            request.Description = response.Description;

            await _requestStore.UpdateByIdAsync(id, request);

            _logger.LogDebug("Successful update for backchannel authentication request id {id}", id);
        }
    }
}
