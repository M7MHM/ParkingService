using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Parking.Application.Behaviors;
using Parking.Application.Features.Bookings.Commands;
using Parking.Application.Features.Parkings.Queries;
using Parking.Application.Interfaces;
using ParkingBooking.Domain.Enums;
using ParkingBooking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Parking.Test.Application
{
    public sealed class CacheBehaviorTests
    {
        private const string VersionKey = "parking:cache:version";

        [Fact]
        public async Task Handle_CacheMiss_ExecutesHandlerAndStoresPayload()
        {
            var cache = CreateMemoryCache();
            await SetVersionAsync(cache, "v1");
            var behavior = CreateBehavior<ProbeQuery, string>(cache);
            var handlerCalls = 0;

            var result = await behavior.Handle(
                new ProbeQuery("one"),
                _ =>
                {
                    handlerCalls++;
                    return Task.FromResult("database-result");
                },
                CancellationToken.None);

            result.Should().Be("database-result");
            handlerCalls.Should().Be(1);
            (await cache.GetStringAsync("parking:query:v1:probe:one"))
                .Should().Be("\"database-result\"");
        }

        [Fact]
        public async Task Handle_CacheHit_ReturnsPayloadWithoutCallingHandler()
        {
            var cache = CreateMemoryCache();
            await SetVersionAsync(cache, "v1");
            await cache.SetStringAsync(
                "parking:query:v1:probe:one",
                "\"cached-result\"",
                ShortEntry());
            var behavior = CreateBehavior<ProbeQuery, string>(cache);

            var result = await behavior.Handle(
                new ProbeQuery("one"),
                _ => Task.FromException<string>(
                    new InvalidOperationException("Handler must not run on a hit.")),
                CancellationToken.None);

            result.Should().Be("cached-result");
        }

        [Fact]
        public async Task Handle_WriteCommand_RotatesVersionAfterSuccess()
        {
            var cache = CreateMemoryCache();
            await SetVersionAsync(cache, "before");
            var behavior = CreateBehavior<ProbeWriteCommand, string>(cache);

            var result = await behavior.Handle(
                new ProbeWriteCommand(),
                _ => Task.FromResult("saved"),
                CancellationToken.None);

            result.Should().Be("saved");
            (await cache.GetStringAsync(VersionKey)).Should().NotBe("before");
        }

        [Fact]
        public async Task Handle_WriteCommand_RotatesVersionEvenWhenHandlerThrows()
        {
            var cache = CreateMemoryCache();
            await SetVersionAsync(cache, "before");
            var behavior = CreateBehavior<ProbeWriteCommand, string>(cache);

            var act = () => behavior.Handle(
                new ProbeWriteCommand(),
                _ => Task.FromException<string>(
                    new InvalidOperationException("Simulated failure after write.")),
                CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            (await cache.GetStringAsync(VersionKey)).Should().NotBe("before");
        }

        [Fact]
        public async Task Handle_DistributedCacheFails_ExecutesHandlerWithoutCache()
        {
            var cache = new FailingDistributedCache();
            var behavior = CreateBehavior<ProbeQuery, string>(cache);
            var handlerCalls = 0;

            var result = await behavior.Handle(
                new ProbeQuery("one"),
                _ =>
                {
                    handlerCalls++;
                    return Task.FromResult("database-result");
                },
                CancellationToken.None);

            result.Should().Be("database-result");
            handlerCalls.Should().Be(1);
        }

        [Fact]
        public async Task Handle_CorruptPayload_UsesHandlerAndReplacesPayload()
        {
            var cache = CreateMemoryCache();
            await SetVersionAsync(cache, "v1");
            await cache.SetStringAsync(
                "parking:query:v1:probe:one",
                "{corrupt-json",
                ShortEntry());
            var behavior = CreateBehavior<ProbeQuery, string>(cache);
            var handlerCalls = 0;

            var result = await behavior.Handle(
                new ProbeQuery("one"),
                _ =>
                {
                    handlerCalls++;
                    return Task.FromResult("fresh-result");
                },
                CancellationToken.None);

            result.Should().Be("fresh-result");
            handlerCalls.Should().Be(1);
            (await cache.GetStringAsync("parking:query:v1:probe:one"))
                .Should().Be("\"fresh-result\"");
        }

        [Fact]
        public void ParkingLotResponse_SystemTextJsonRoundTripsValueObjects()
        {
            var expected = new ParkingLotResponse(
                Guid.NewGuid(),
                "Central Garage",
                "Downtown",
                new Location(30.0444, 31.2357, "Cairo"),
                new Money(50m, "EGP"),
                20,
                17,
                true,
                TimeSpan.FromHours(8),
                TimeSpan.FromHours(18),
                DateTime.UtcNow);

            var json = JsonSerializer.Serialize(expected, JsonOptions());
            var actual = JsonSerializer.Deserialize<ParkingLotResponse>(
                json,
                JsonOptions());

            actual.Should().NotBeNull();
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void AvailableSpotsResponse_SystemTextJsonRoundTripsSpotTypeAndMoney()
        {
            IReadOnlyList<ParkingSpotResponse> expected =
            [
                new ParkingSpotResponse(
                Guid.NewGuid(),
                "A-01",
                new SpotType(SpotSize.Compact, hasElectricCharger: true, isCovered: true),
                SpotStatus.Available,
                new Money(25m, "EGP"),
                null,
                null)
            ];

            var json = JsonSerializer.Serialize(expected, JsonOptions());
            var actual =
                JsonSerializer.Deserialize<IReadOnlyList<ParkingSpotResponse>>(
                    json,
                    JsonOptions());

            actual.Should().NotBeNull();
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void ApplicationCommands_AllWritesAreMarkedOrExplicitlyAllowListed()
        {
            var allowedWithoutInvalidation = new HashSet<string>(StringComparer.Ordinal)
        {
            "LoginCommand",
            "RegisterCommand",
            "ReleaseSpotCommand", 
            "ReserveSpotCommand"
        };

            var requestTypes = typeof(CreateBookingCommand).Assembly
                .GetTypes()
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    type.Name.EndsWith("Command", StringComparison.Ordinal))
                .Where(type => type.GetInterfaces().Any(contract =>
                    contract.Namespace == "MediatR" &&
                    contract.Name.StartsWith("IRequest", StringComparison.Ordinal)));

            var unmarked = requestTypes
                .Where(type =>
                    !typeof(ICacheInvalidatingRequest).IsAssignableFrom(type) &&
                    !allowedWithoutInvalidation.Contains(type.Name))
                .Select(type => type.FullName)
                .ToArray();

            unmarked.Should().BeEmpty(
                "each new command that can change cached parking data must opt into invalidation; "
                + "commands that cannot affect it need an explicit allow-list entry");
        }

        private static JsonSerializerOptions JsonOptions() =>
            new(JsonSerializerDefaults.Web);

        private static IDistributedCache CreateMemoryCache() =>
            new MemoryDistributedCache(
                Options.Create(new MemoryDistributedCacheOptions()));

        private static Task SetVersionAsync(IDistributedCache cache, string version) =>
            cache.SetStringAsync(
                VersionKey,
                version,
                new DistributedCacheEntryOptions());

        private static DistributedCacheEntryOptions ShortEntry() =>
            new()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            };

        private static CacheBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
            IDistributedCache cache)
            where TRequest : notnull =>
            new(
                cache,
                Mock.Of<ILogger<CacheBehavior<TRequest, TResponse>>>());

        public sealed record ProbeQuery(string Id) : ICacheableQuery<string>
        {
            public string CacheKey => $"probe:{Id}";
            public string? CacheGroup => null;
            public TimeSpan CacheDuration => TimeSpan.FromMinutes(1);
        }

        public sealed record ProbeWriteCommand : ICacheInvalidatingRequest
        {
            public string? CacheGroup => null;
        }

        private sealed class FailingDistributedCache : IDistributedCache
        {
            public byte[]? Get(string key) =>
                throw new IOException("Simulated Redis outage.");

            public Task<byte[]?> GetAsync(
                string key,
                CancellationToken token = default) =>
                Task.FromException<byte[]?>(
                    new IOException("Simulated Redis outage."));

            public void Refresh(string key) { }

            public Task RefreshAsync(
                string key,
                CancellationToken token = default) =>
                Task.CompletedTask;

            public void Remove(string key) { }

            public Task RemoveAsync(
                string key,
                CancellationToken token = default) =>
                Task.CompletedTask;

            public void Set(
                string key,
                byte[] value,
                DistributedCacheEntryOptions options)
            { }

            public Task SetAsync(
                string key,
                byte[] value,
                DistributedCacheEntryOptions options,
                CancellationToken token = default) =>
                Task.CompletedTask;
        }
    }
}
