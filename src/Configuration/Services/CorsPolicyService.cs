// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Configuration.Repositories;
using Duende.IdentityServer.Services;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Configuration.Services;

/// <summary>
/// Implementation of ICorsPolicyService that consults the client configuration repository for allowed CORS origins.
/// </summary>
/// <seealso cref="ICorsPolicyService" />
public class CorsPolicyService : ICorsPolicyService
{
    private readonly IClientRepository _clientRepository;
    private readonly ILogger<CorsPolicyService> _logger;
    private readonly ICancellationTokenProvider _cancellationTokenProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorsPolicyService"/> class.
    /// </summary>
    /// <param name="clientRepository">The Client Repository</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationTokenProvider"></param>
    /// <exception cref="ArgumentNullException">context</exception>
    public CorsPolicyService(
        IClientRepository clientRepository,
        ILogger<CorsPolicyService> logger,
        ICancellationTokenProvider cancellationTokenProvider)
    {
        _clientRepository = clientRepository;
        _logger = logger;
        _cancellationTokenProvider = cancellationTokenProvider;
    }

    /// <inheritdoc/>
    public async Task<bool> IsOriginAllowedAsync(string origin)
    {
        var isAllowed = await _clientRepository.CorsOriginExists(origin, _cancellationTokenProvider.CancellationToken);
        _logger.LogDebug("Origin {origin} is allowed: {originAllowed}", origin, isAllowed);
        return isAllowed;
    }
}