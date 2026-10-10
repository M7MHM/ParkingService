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
        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            IDomainEventDispatcher eventDispatcher) : base(options)
        {
            _eventDispatcher = eventDispatcher;
        }

        public DbSet<ParkingLot> ParkingLots => Set<ParkingLot>();
        public DbSet<ParkingSpot> ParkingSpots => Set<ParkingSpot>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<User> Users => Set<User>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var (entities, events) = CollectDomainEvents(); 

            var result = await base.SaveChangesAsync(cancellationToken); 

            foreach (var entity in entities)
                entity.ClearDomainEvents(); 

            foreach (var domainEvent in events)
            {
                await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken); 
            }

            return result;
        }
        public override int SaveChanges()
        {
            var (entities, events) = CollectDomainEvents(); 

            var result = base.SaveChanges();

            foreach (var entity in entities)
                entity.ClearDomainEvents(); 

            foreach (var domainEvent in events)
            {
                _eventDispatcher.DispatchAsync(domainEvent, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
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