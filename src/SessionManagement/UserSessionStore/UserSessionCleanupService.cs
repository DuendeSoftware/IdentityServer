// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

namespace Duende.SessionManagement;



// todo
//public class TicketCleanupService : IHostedService
//{
//    private readonly IServiceProvider _serviceProvider;
//    private readonly SessionManagementOptions _options;
//    private readonly ILogger<TicketCleanupService> _logger;

//    private CancellationTokenSource _source;

//    public TicketCleanupService(
//        IServiceProvider serviceProvider,
//        SessionManagementOptions options,
//        ILogger<TicketCleanupService> logger)
//    {
//        _serviceProvider = serviceProvider;
//        _options = options;
//        _logger = logger;
//    }

//    public Task StartAsync(CancellationToken cancellationToken)
//    {
//        if (_options.EnableSessionCleanupInterval)
//        {
//            if (_source != null) throw new InvalidOperationException("Already started. Call Stop first.");

//            _logger.LogDebug("Starting ticket cleanup");

//            _source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

//            Task.Factory.StartNew(() => StartInternalAsync(_source.Token));
//        }

//        return Task.CompletedTask;
//    }

//    public Task StopAsync(CancellationToken cancellationToken)
//    {
//        if (_options.EnableSessionCleanupInterval)
//        {
//            if (_source == null) throw new InvalidOperationException("Not started. Call Start first.");

//            _logger.LogDebug("Stopping ticket cleanup");

//            _source.Cancel();
//            _source = null;
//        }

//        return Task.CompletedTask;
//    }

//    private async Task StartInternalAsync(CancellationToken cancellationToken)
//    {
//        while (true)
//        {
//            if (cancellationToken.IsCancellationRequested)
//            {
//                _logger.LogDebug("CancellationRequested. Exiting.");
//                break;
//            }

//            try
//            {
//                await Task.Delay(_options.SessionCleanupInterval, cancellationToken);
//            }
//            catch (TaskCanceledException)
//            {
//                _logger.LogDebug("TaskCanceledException. Exiting.");
//                break;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError("Task.Delay exception: {0}. Exiting.", ex.Message);
//                break;
//            }

//            if (cancellationToken.IsCancellationRequested)
//            {
//                _logger.LogDebug("CancellationRequested. Exiting.");
//                break;
//            }

//            await RemoveExpiredTicketsAsync();
//        }
//    }

//    private async Task RemoveExpiredTicketsAsync()
//    {
//        try
//        {
//            _logger.LogTrace("Querying for expired tickets to remove");

//            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
//            {
//                using (var context = serviceScope.ServiceProvider.GetService<SessionManagementDbContext>())
//                {
//                    await RemoveExpiredTicketsAsync(context);
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError("Exception removing expired tickets: {exception}", ex.Message);
//        }
//    }

//    private async Task RemoveExpiredTicketsAsync(SessionManagementDbContext context)
//    {
//        var found = Int32.MaxValue;

//        while (found >= _options.SessionCleanupBatchSize)
//        {
//            var expiredItems = await context.UserSessions
//                .Where(x => x.Expires < DateTime.UtcNow)
//                .OrderBy(x => x.Id)
//                .Take(_options.SessionCleanupBatchSize)
//                .ToArrayAsync();

//            found = expiredItems.Length;
//            _logger.LogInformation("Removing {expiredItems} tickets", found);

//            if (found > 0)
//            {
//                context.UserSessions.RemoveRange(expiredItems);
//                try
//                {
//                    await context.SaveChangesAsync();
//                }
//                catch (DbUpdateConcurrencyException ex)
//                {
//                    // we get this if/when someone else already deleted the records
//                    // we want to essentially ignore this, and keep working
//                    _logger.LogDebug("Concurrency exception removing expired tickets: {exception}", ex.Message);
//                }
//            }
//        }
//    }
//}
