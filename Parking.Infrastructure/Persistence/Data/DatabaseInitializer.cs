using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Parking.Application.Interfaces;
using Parking.Domain.Entities;
using Parking.Domain.Enums;
using Parking.Domain.ValueObjects;
using ParkingBooking.Domain.Entities;
using ParkingBooking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Infrastructure.Persistence.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            var db = provider.GetRequiredService<AppDbContext>();

            await db.Database.MigrateAsync(ct);

            await SeedAdminAsync(provider, ct);
            await DevDataSeeder.SeedAsync(provider, ct);
        }

        private static async Task SeedAdminAsync(IServiceProvider provider, CancellationToken ct)
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var email = config["Seed:AdminEmail"];
            var password = config["Seed:AdminPassword"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return;

            if (password.Length < 15)
                throw new InvalidOperationException("Admin password must be at least 15 characters long.");

            var users = provider.GetRequiredService<IUserRepository>();
            if (await users.GetByEmailAsync(email, ct) is not null)
                return;

            var hasher = provider.GetRequiredService<IPasswordHasher>();
            var unitOfWork = provider.GetRequiredService<IUnitOfWork>();

            var admin = new User(
                new Email(email),
                hasher.Hash(password),
                config["Seed:AdminName"] ?? "Administrator",
                UserRole.Admin);

            await users.AddAsync(admin, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
    }

    public static class DevDataSeeder
    {
        private const string LotNamePrefix = "Seed Lot";
        private const string SeedEmailDomain = "seed.local";
        private const int DefaultUserCount = 1000;
        private const int MaxUserCount = 5000; 

        private sealed record LotPlan(string Name, int SpotCount, int OpenHour, int CloseHour, decimal HourlyRate);

        public static async Task SeedAsync(IServiceProvider provider, CancellationToken ct = default)
        {
            var config = provider.GetRequiredService<IConfiguration>();

            if (!bool.TryParse(config["Seed:DevData"], out var enabled) || !enabled)
                return;

            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DevDataSeeder");
            var db = provider.GetRequiredService<AppDbContext>();

            if (await db.ParkingLots.AnyAsync(l => l.Name.StartsWith(LotNamePrefix), ct))
            {
                logger.LogInformation(
                    "Dev dataset already present (lots named '{Prefix}*'); skipping.", LotNamePrefix);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var now = DateTime.UtcNow;
            var rng = new Random(20261101);

            var users = BuildUsers(ReadUserCount(config), BuildSharedPasswordHash(provider, config, logger));
            var userIds = users.Select(u => u.Id).ToArray();

            var lots = new List<ParkingLot>();
            var bookings = new List<Booking>();

            var plans = BuildLotPlans();
            for (var i = 0; i < plans.Count; i++)
                lots.Add(BuildLot(plans[i], i, rng, now, userIds, bookings, logger));

            foreach (var booking in bookings) booking.ClearDomainEvents();

            db.ChangeTracker.AutoDetectChangesEnabled = false;
            try
            {
                await using var transaction = await db.Database.BeginTransactionAsync(ct);

                await SaveInChunksAsync(db, users, 500, ct);
                await SaveInChunksAsync(db, lots, 1, ct);        
                await SaveInChunksAsync(db, bookings, 500, ct);

                await transaction.CommitAsync(ct);
            }
            finally
            {
                db.ChangeTracker.AutoDetectChangesEnabled = true;
            }

            stopwatch.Stop();

            var xl = lots.First();
            var medium = lots.First(l => l.Name.StartsWith("Seed Lot M"));
            logger.LogInformation(
                "Dev dataset ready in {Elapsed:n1}s: {Users} users, {Lots} lots, {Spots} spots, {Bookings} bookings.",
                stopwatch.Elapsed.TotalSeconds, users.Count, lots.Count, lots.Sum(l => l.Spots.Count), bookings.Count);
            logger.LogInformation(
                "Test IDs → XL lot (1500 spots): {XlId} | medium lot (100 spots): {MediumId}", xl.Id, medium.Id);
        }
        private static int ReadUserCount(IConfiguration config)
        {
            var count = int.TryParse(config["Seed:DevUserCount"], out var parsed) ? parsed : DefaultUserCount;
            return Math.Clamp(count, 1, MaxUserCount);
        }

        private static string BuildSharedPasswordHash(IServiceProvider provider, IConfiguration config, ILogger logger)
        {
            var password = config["Seed:DevUserPassword"];

            if (string.IsNullOrWhiteSpace(password))
            {
                password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
                logger.LogInformation(
                    "Seed:DevUserPassword not set: seeded users got a random password nobody knows.");
            }
            else if (password.Length < 15)
            {
                throw new InvalidOperationException("Seed:DevUserPassword must be at least 15 characters long.");
            }

            return provider.GetRequiredService<IPasswordHasher>().Hash(password);
        }
        private static List<User> BuildUsers(int count, string passwordHash)
        {
            var users = new List<User>(count);

            for (var i = 1; i <= count; i++)
            {
                users.Add(new User(
                    new Email($"seed.user{i:D4}@{SeedEmailDomain}"),
                    passwordHash,
                    $"Seed User {i:D4}",
                    UserRole.Customer));
            }

            return users;
        }
        private static List<LotPlan> BuildLotPlans()
        {
            var plans = new List<LotPlan>
            {
                new($"{LotNamePrefix} XL - Downtown", 1500, 0, 24, 40m)
            };

            for (var i = 1; i <= 4; i++)
                plans.Add(new($"{LotNamePrefix} L{i} - District {i}", 300, 6, 24, 30m + i * 2));

            for (var i = 1; i <= 10; i++)
                plans.Add(new($"{LotNamePrefix} M{i} - Mall {i}", 100, 8, 23, 20m + i));

            for (var i = 1; i <= 15; i++)
                plans.Add(new($"{LotNamePrefix} S{i} - Street {i}", 40, 7, 22, 10m + i));

            return plans;
        }
        private static ParkingLot BuildLot(
            LotPlan plan,
            int lotIndex,
            Random rng,
            DateTime now,
            Guid[] userIds,
            List<Booking> bookings,
            ILogger logger)
        {
            var lot = new ParkingLot(
                plan.Name,
                "Dev seed data",
                new Location(29.95 + rng.NextDouble() * 0.2, 31.15 + rng.NextDouble() * 0.2, $"Seed address {lotIndex + 1}"),
                new Money(plan.HourlyRate),
                plan.SpotCount,
                TimeSpan.FromHours(plan.OpenHour),
                TimeSpan.FromHours(plan.CloseHour));

            var spots = new List<ParkingSpot>(plan.SpotCount);
            for (var i = 0; i < plan.SpotCount; i++)
            {
                var (type, multiplier) = PickSpotType(i);
                var spot = new ParkingSpot(
                    $"S{lotIndex + 1:D2}-{i + 1:D4}",
                    type,
                    new Money(plan.HourlyRate * multiplier),
                    lot.Id);

                lot.AddSpot(spot);
                spots.Add(spot);
            }

            var order = Enumerable.Range(0, spots.Count).OrderBy(_ => rng.Next()).ToArray();
            var occupiedCount = (int)(spots.Count * 0.08);
            var reservedCount = (int)(spots.Count * 0.55);
            var maintenanceCount = Math.Max(1, (int)(spots.Count * 0.01));
            var cursor = 0;

            for (var n = 0; n < occupiedCount; n++)
            {
                var spot = spots[order[cursor++]];
                var start = now.AddMinutes(-rng.Next(20, 90));
                var end = now.AddMinutes(rng.Next(60, 180));

                var booking = NewBooking(spot, RandomUser(rng, userIds), start, end, confirm: true);
                lot.ReserveSpot(spot.Id, booking.Id, end);
                lot.MarkSpotOccupied(spot.Id);
                bookings.Add(booking);
            }

            for (var n = 0; n < reservedCount; n++)
            {
                var spot = spots[order[cursor++]];
                var start = now.AddHours(rng.Next(1, 120)).AddMinutes(rng.Next(0, 60));
                var end = start.AddHours(rng.Next(1, 8));

                var booking = NewBooking(spot, RandomUser(rng, userIds), start, end, confirm: true);
                lot.ReserveSpot(spot.Id, booking.Id, end);
                bookings.Add(booking);
            }

            for (var n = 0; n < maintenanceCount; n++)
                lot.SetSpotMaintenance(spots[order[cursor++]].Id);

            var historyCount = (int)(spots.Count * 0.30);
            for (var n = 0; n < historyCount; n++)
            {
                var spot = spots[rng.Next(spots.Count)];
                var userId = RandomUser(rng, userIds);
                var roll = rng.NextDouble();

                if (roll < 0.70)
                {
                    var start = now.AddDays(-rng.Next(1, 30)).AddHours(-rng.Next(0, 12));
                    var booking = NewBooking(spot, userId, start, start.AddHours(rng.Next(1, 7)), confirm: true);
                    booking.Complete();
                    bookings.Add(booking);
                }
                else if (roll < 0.95)
                {
                    var start = now.AddDays(rng.Next(1, 10)).AddHours(rng.Next(0, 12));
                    var booking = NewBooking(spot, userId, start, start.AddHours(rng.Next(1, 7)), confirm: true);
                    booking.Cancel();
                    bookings.Add(booking);
                }
                else
                {
                    var start = now.AddDays(-rng.Next(1, 30));
                    var booking = NewBooking(spot, userId, start, start.AddHours(rng.Next(1, 7)), confirm: false);
                    booking.MarkExpired();
                    bookings.Add(booking);
                }
            }

            var actuallyAvailable = spots.Count(s => s.IsCurrentlyAvailable());
            if (lot.AvailableSpotsCount != actuallyAvailable)
            {
                logger.LogWarning(
                    "Lot '{Lot}': AvailableSpotsCount={Counter} but {Actual} spots are actually available.",
                    plan.Name, lot.AvailableSpotsCount, actuallyAvailable);
            }

            lot.ClearDomainEvents();
            foreach (var spot in spots) spot.ClearDomainEvents();

            return lot;
        }
        private static (SpotType Type, decimal RateMultiplier) PickSpotType(int index)
        {
            if (index % 25 == 2) return (SpotType.VIP(), 2m);
            if (index % 50 == 0) return (SpotType.Electric(), 1.5m);
            if (index % 40 == 1) return (SpotType.Accessible(), 1m);
            return (SpotType.Standard(), 1m);
        }
        private static Guid RandomUser(Random rng, Guid[] userIds) => userIds[rng.Next(userIds.Length)];

        private static Booking NewBooking(ParkingSpot spot, Guid userId, DateTime start, DateTime end, bool confirm)
        {
            var period = new BookingPeriod(start, end);
            var price = spot.CalculatePrice(period.WholeHoursCeiling());

            var booking = new Booking(spot.Id, userId, period, price, null);
            if (confirm)
                booking.Confirm();

            return booking;
        }
        private static async Task SaveInChunksAsync<T>(
            AppDbContext db, IEnumerable<T> items, int chunkSize, CancellationToken ct) where T : class
        {
            foreach (var chunk in items.Chunk(chunkSize))
            {
                db.Set<T>().AddRange(chunk);
                await db.SaveChangesAsync(ct);

                db.ChangeTracker.Clear();
            }
        }
    }
}
