using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Parking.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Parking.Application.Behaviors
{
    public sealed class CacheBehavior<TRequest, TResponse>
            : IPipelineBehavior<TRequest, TResponse>
            where TRequest : notnull
    {
        private const string CacheVersionKey = "parking:cache:version";
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        private readonly IDistributedCache _cache;
        private readonly ILogger<CacheBehavior<TRequest, TResponse>> _logger;

        public CacheBehavior(
            IDistributedCache cache,
            ILogger<CacheBehavior<TRequest, TResponse>> logger)
        {
            _cache = cache;
            _logger = logger;
        }
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is ICacheInvalidatingRequest)
            {
                try
                {
                    return await next();
                }
                finally
                {
                    await TryRotateCacheVersionAsync(CancellationToken.None);
                }
            }
            if (request is not ICacheableQuery<TResponse> query ||
                query.CacheDuration <= TimeSpan.Zero ||
                string.IsNullOrWhiteSpace(query.CacheKey))
            {
                return await next();
            }
            var key = await TryBuildVersionedKeyAsync(query.CacheKey, cancellationToken);
            if (key is not null)
            {
                var payload = await TryGetPayloadAsync(key, cancellationToken);
                if (payload is not null)
                {
                    try
                    {
                        var cached = JsonSerializer.Deserialize<TResponse>(payload, JsonOptions);
                        _logger.LogDebug("Cache hit for query {QueryName}.", typeof(TRequest).Name);
                        return cached!;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception) when (exception is not OutOfMemoryException)
                    {
                        _logger.LogWarning(exception,
                            "Cached payload for query {QueryName} is unreadable; loading from the handler.",
                            typeof(TRequest).Name);
                        await TryRemoveAsync(key, cancellationToken);
                    }
                }
                _logger.LogDebug("Cache miss for query {QueryName}.", typeof(TRequest).Name);
            }
            var response = await next();

            if (key is not null)
                await TryStorePayloadAsync(key, response, query.CacheDuration, cancellationToken);

            return response;
        }
        private async Task<string?> TryBuildVersionedKeyAsync(
            string queryKey,
            CancellationToken cancellationToken)
        {
            try
            {
                var version = await _cache.GetStringAsync(CacheVersionKey, cancellationToken);
                if (string.IsNullOrWhiteSpace(version))
                {
                    version = Guid.NewGuid().ToString("N");
                    await _cache.SetStringAsync(CacheVersionKey, version,
                        new DistributedCacheEntryOptions(), cancellationToken);
                }

                return $"parking:query:{version}:{queryKey}";
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                _logger.LogWarning(exception,
                    "Could not prepare cache for query {QueryName}; executing without cache.",
                    typeof(TRequest).Name);
                return null;
            }
        }
        private async Task<string?> TryGetPayloadAsync(
            string key,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _cache.GetStringAsync(key, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                _logger.LogWarning(exception,
                    "Cache read failed for query {QueryName}; executing without cache.",
                    typeof(TRequest).Name);
                return null;
            }
        }
        private async Task TryStorePayloadAsync(
            string key,
            TResponse response,
            TimeSpan duration,
            CancellationToken cancellationToken)
        {
            try
            {
                var payload = JsonSerializer.Serialize(response, JsonOptions);
                await _cache.SetStringAsync(key, payload,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = duration
                    },
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                _logger.LogWarning(exception,
                    "Cache write failed for query {QueryName}; returning the handler result.",
                    typeof(TRequest).Name);
            }
        }

        private async Task TryRemoveAsync(string key, CancellationToken cancellationToken)
        {
            try
            {
                await _cache.RemoveAsync(key, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                _logger.LogWarning(exception, "Could not remove an unreadable cache entry.");
            }
        }

        private async Task TryRotateCacheVersionAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _cache.SetStringAsync(CacheVersionKey, Guid.NewGuid().ToString("N"),
                    new DistributedCacheEntryOptions(), cancellationToken);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                _logger.LogWarning(exception,
                    "Could not invalidate parking cache; entries will expire by their TTL.");
            }
        }
    }
}
