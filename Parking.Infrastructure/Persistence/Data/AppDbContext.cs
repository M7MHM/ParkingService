using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Parking.Application.Interfaces;
using Parking.Domain.Entities;
using Parking.Infrastructure.Persistence.Configurations;
using ParkingBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Infrastructure.Persistence.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IDomainEventDispatcher _eventDispatcher;
        public AppDbContext( DbContextOptions<AppDbContext> options,
            IDomainEventDispatcher eventDispatcher) : base(options)
        {
            _eventDispatcher = eventDispatcher;
        }

        public DbSet<ParkingLot> ParkingLots => Set<ParkingLot>();
        public DbSet<ParkingSpot> ParkingSpots => Set<ParkingSpot>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<User> Users => Set<User>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken: cancellationToken);

        public override async Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            var (entities, domainEvents) = CollectDomainEvents();

            foreach (var domainEvent in domainEvents)
                await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);

            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            foreach (var entity in entities)
                entity.ClearDomainEvents();

            return result;
        }

        public override int SaveChanges() =>
            SaveChanges(acceptAllChangesOnSuccess: true);

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var (entities, domainEvents) = CollectDomainEvents();

            foreach (var domainEvent in domainEvents)
                _eventDispatcher.DispatchAsync(domainEvent, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

            var result = base.SaveChanges(acceptAllChangesOnSuccess);

            foreach (var entity in entities)
                entity.ClearDomainEvents();

            return result;
        }
        private (List<BaseEntity> Entities, List<object> Events) CollectDomainEvents()
        {
            var entities = ChangeTracker.Entries<BaseEntity>()
                .Select(entry => entry.Entity)
                .Where(entity => entity.DomainEvents.Count > 0)
                .ToList();

            var events = entities.SelectMany(entity => entity.DomainEvents).ToList();
            return (entities, events);
        }
    }
}