// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using System;
using Duende.IdentityServer.EntityFramework;
using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Helper to cleanup expired persisted grants.
/// </summary>
public class TokenCleanupHost : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly OperationalStoreOptions _options;
    private readonly ILogger<TokenCleanupHost> _logger;

    private TimeSpan CleanupInterval => TimeSpan.FromSeconds(_options.TokenCleanupInterval);
        
    private CancellationTokenSource _source;

    /// <summary>
    /// Constructor for TokenCleanupHost.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    public TokenCleanupHost(IServiceProvider serviceProvider, OperationalStoreOptions options, ILogger<TokenCleanupHost> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    /// <summary>
    /// Starts the token cleanup polling.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.EnableTokenCleanup)
        {
            if (_source != null) throw new InvalidOperationException("Already started. Call Stop first.");

            _logger.LogDebug("Starting grant removal");

            _source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Task.Factory.StartNew(() => StartInternalAsync(_source.Token), cancellationToken);
        }
            
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the token cleanup polling.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_options.EnableTokenCleanup)
        {
            if (_source == null) throw new InvalidOperationException("Not started. Call Start first.");

            _logger.LogDebug("Stopping grant removal");

            _source.Cancel();
            _source = null;
        }

        return Task.CompletedTask;
    }

    private async Task StartInternalAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("CancellationRequested. Exiting.");
                break;
            }

            try
            {
                await Task.Delay(CleanupInterval, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogDebug("TaskCanceledException. Exiting.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError("Task.Delay exception: {0}. Exiting.", ex.Message);
                break;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("CancellationRequested. Exiting.");
                break;
            }

            await RemoveExpiredGrantsAsync(cancellationToken);
        }
    }

    async Task RemoveExpiredGrantsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var tokenCleanupService = serviceScope.ServiceProvider.GetRequiredService<TokenCleanupService>();
                await tokenCleanupService.RemoveExpiredGrantsAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception removing expired grants: {exception}", ex.Message);
        }
    }
}