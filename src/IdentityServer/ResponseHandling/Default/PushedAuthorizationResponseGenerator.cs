// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;
using IdentityModel;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.ResponseHandling;

/// <inheritdoc />
public class PushedAuthorizationResponseGenerator : IPushedAuthorizationResponseGenerator
{
    private readonly IHandleGenerationService _handleGeneration;
    private readonly IPushedAuthorizationRequestStore _store;
    private readonly IdentityServerOptions _options;
    private readonly IPushedAuthorizationService _pushedAuthorizationService;
    private readonly ILogger<PushedAuthorizationResponseGenerator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PushedAuthorizationResponseGenerator"/> class.
    /// </summary>
    /// <param name="handleGeneration">The handle generation service, used for creation of request uri reference values.
    /// </param>
    /// <param name="store">The pushed authorization request store</param>
    /// <param name="options">The IdentityServer options</param>
    /// <param name="pushedAuthorizationService">The pushed authorization service</param>
    /// <param name="logger">The logger</param>
    public PushedAuthorizationResponseGenerator(IHandleGenerationService handleGeneration,
        IPushedAuthorizationRequestStore store,
        IdentityServerOptions options,
        IPushedAuthorizationService pushedAuthorizationService,
        ILogger<PushedAuthorizationResponseGenerator> logger)
    {
        _handleGeneration = handleGeneration;
        _store = store;
        _options = options;
        _pushedAuthorizationService = pushedAuthorizationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PushedAuthorizationResponse> CreateResponseAsync(ValidatedPushedAuthorizationRequest request)
    {
        // Create a reference value
        var referenceValue = await _handleGeneration.GenerateAsync(); 
        
        var requestUri = $"{IdentityServerConstants.PushedAuthorizationRequestUri}:{referenceValue}";
        
        // Calculate the expiration
        var expiration = request.Client.PushedAuthorizationLifetime ?? _options.PushedAuthorization.Lifetime;
    
        // serialize and data protect the parameters
        var protectedData = _pushedAuthorizationService.Serialize(request.Raw);

        // Persist 
        await _store.StoreAsync(new Models.PushedAuthorizationRequest
        {
            ReferenceValue = referenceValue,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiration),
            Parameters = protectedData
        });

        // TODO - Catch errors and return PushedAuthorizationFailure?

        // Return reference and expiration
        return new PushedAuthorizationSuccess
        {
            RequestUri = requestUri,
            ExpiresIn = expiration
        };
    }
}