using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Notification
{
    internal interface IIdempotencyStore
    {
        Task<bool> WasProcessedAsync(Guid messageId, CancellationToken ct);
        Task MarkProcessedAsync(Guid messageId, CancellationToken ct);
    }
    internal sealed class RedisIdempotencyStore : IIdempotencyStore
    {
        private static readonly TimeSpan Retention = TimeSpan.FromDays(7);
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisIdempotencyStore> _logger;

        public RedisIdempotencyStore(
            IDistributedCache cache,
            ILogger<RedisIdempotencyStore> logger)
        {
            _cache = cache;
            _logger = logger;
        }
        public async Task<bool> WasProcessedAsync(Guid messageId, CancellationToken ct)
        {
            try
            {
                return await _cache.GetAsync(Key(messageId), ct) is not null;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis unavailable; treating {MessageId} as not processed.", messageId);
                return false;
            }
        }
        public async Task MarkProcessedAsync(Guid messageId, CancellationToken ct)
        {
            try
            {
                await _cache.SetAsync(
                    Key(messageId),
                    new byte[] { 1 },
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = Retention
                    },
                    ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not record {MessageId}; a duplicate email is possible on redelivery.", messageId);
            }
        }
        private static string Key(Guid messageId) => $"processed:{messageId:N}";
    }
}
