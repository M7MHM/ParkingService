using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Infrastructure.Persistence.Configurations
{
    public class ParkingLotConfiguration : IEntityTypeConfiguration<ParkingLot>
    {
        public void Configure(EntityTypeBuilder<ParkingLot> builder)
        {
            builder.ToTable("ParkingLots");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Description)
                .HasMaxLength(1000);

            builder.OwnsOne(p => p.Location, location =>
            {
                location.Property(l => l.Latitude).HasColumnName("Latitude");
                location.Property(l => l.Longitude).HasColumnName("Longitude");
                location.Property(l => l.Address).HasColumnName("Address").HasMaxLength(500);
            });

            builder.OwnsOne(p => p.HourlyRate, money =>
            {
                money.Property(m => m.Amount).HasColumnName("HourlyRateAmount").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("HourlyRateCurrency").HasMaxLength(3);
            });

            builder.Property(p => p.TotalSpots);
            builder.Property(p => p.AvailableSpotsCount);
            builder.Property(p => p.IsActive);
            builder.Property(p => p.OpeningTime);
            builder.Property(p => p.ClosingTime);
            builder.Property(p => p.CreatedAt);
            builder.Property(p => p.UpdatedAt);
            builder.Property(p => p.IsDeleted);

            builder.HasQueryFilter(p => !p.IsDeleted);

            builder.HasMany(p => p.Spots)
                .WithOne(s => s.ParkingLot)
                .HasForeignKey(s => s.ParkingLotId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
