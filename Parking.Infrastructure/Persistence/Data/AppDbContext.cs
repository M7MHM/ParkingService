using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            IDomainEventDispatcher eventDispatcher) : base(options)
        {
            _eventDispatcher = eventDispatcher;
        }

        public DbSet<ParkingLot> ParkingLots => Set<ParkingLot>();
        public DbSet<ParkingSpot> ParkingSpots => Set<ParkingSpot>();
        public DbSet<Booking> Bookings => Set<Booking>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new ParkingSpotConfiguration());
            modelBuilder.ApplyConfiguration(new ParkingSpotConfiguration());
            modelBuilder.ApplyConfiguration(new BookingConfiguration());
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var domainEvents = CollectAndClearDomainEvents();

            var result = await base.SaveChangesAsync(cancellationToken);

            foreach (var domainEvent in domainEvents)
            {
                await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
            }

            return result;
        }
        public override int SaveChanges()
        {
            var domainEvents = CollectAndClearDomainEvents();

            var result = base.SaveChanges();

            foreach (var domainEvent in domainEvents)
            {
                _eventDispatcher.DispatchAsync(domainEvent, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }

            return result;
        }

        private List<object> CollectAndClearDomainEvents()
        {
            var trackedEntities = ChangeTracker.Entries<BaseEntity>().ToList();

            var domainEvents = trackedEntities
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();

            trackedEntities.ForEach(e => e.Entity.ClearDomainEvents());

            return domainEvents;
        }
    }
}
