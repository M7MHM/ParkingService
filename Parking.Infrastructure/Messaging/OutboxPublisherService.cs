using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Parking.Infrastructure.Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Infrastructure.Messaging
{
    internal sealed class OutboxPublisherService : BackgroundService
    {
        private const int BatchSize = 50;
        private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan FailureDelay = TimeSpan.FromSeconds(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RabbitMqPublisher _publisher;
        private readonly ILogger<OutboxPublisherService> _logger;

        public OutboxPublisherService(
            IServiceScopeFactory scopeFactory,
            RabbitMqPublisher publisher,
            ILogger<OutboxPublisherService> logger)
        {
            _scopeFactory = scopeFactory;
            _publisher = publisher;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox publisher started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = IdleDelay;

                try
                {
                    var (published, failed) = await PublishPendingAsync(stoppingToken);

                    if (failed) delay = FailureDelay;
                    else if (published > 0) delay = TimeSpan.Zero;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox publisher iteration failed; retrying shortly.");
                    delay = FailureDelay;
                }

                if (delay > TimeSpan.Zero)
                {
                    try { await Task.Delay(delay, stoppingToken); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }
        private async Task<(int Published, bool Failed)> PublishPendingAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var batch = await db.OutboxMessages
                .Where(m => m.ProcessedAtUtc == null)
                .OrderBy(m => m.OccurredAtUtc)
                .ThenBy(m => m.Id)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (batch.Count == 0)
                return (0, false);

            var published = 0;
            var failed = false;

            try
            {
                foreach (var message in batch)
                {
                    try
                    {
                        await _publisher.PublishAsync(message.RoutingKey, message.Id, message.Payload, ct);

                        message.ProcessedAtUtc = DateTime.UtcNow;
                        published++;

                        _logger.LogInformation("Published {RoutingKey} message {MessageId}.",
                            message.RoutingKey, message.Id);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        message.Attempts++;
                        message.LastError = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                        failed = true;

                        _logger.LogWarning(ex, "Publishing {MessageId} failed (attempt {Attempts}); will retry.",
                            message.Id, message.Attempts);

                        break;
                    }
                }
            }
            finally
            {
                await db.SaveChangesAsync(CancellationToken.None);
            }
            return (published, failed);
        }
    }
}
