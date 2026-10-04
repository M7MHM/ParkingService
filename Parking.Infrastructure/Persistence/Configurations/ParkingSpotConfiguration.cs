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
    public class ParkingSpotConfiguration : IEntityTypeConfiguration<ParkingSpot>
    {
        public void Configure(EntityTypeBuilder<ParkingSpot> builder)
        {
            builder.ToTable("ParkingSpots");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.SpotNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.OwnsOne(s => s.Type, type =>
            {
                type.Property(t => t.Size).HasColumnName("SpotSize").HasConversion<string>();
                type.Property(t => t.HasElectricCharger).HasColumnName("HasElectricCharger");
                type.Property(t => t.IsCovered).HasColumnName("IsCovered");
                type.Property(t => t.IsAccessible).HasColumnName("IsAccessible");
            });

            builder.Property(s => s.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.OwnsOne(s => s.HourlyRate, money =>
            {
                money.Property(m => m.Amount).HasColumnName("HourlyRateAmount").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("HourlyRateCurrency").HasMaxLength(3);
            });

            builder.Property(s => s.ParkingLotId);
            builder.Property(s => s.CurrentBookingId);
            builder.Property(s => s.ReservedUntil);
            builder.Property(s => s.CreatedAt);
            builder.Property(s => s.UpdatedAt);
            builder.Property(s => s.IsDeleted);

            builder.HasQueryFilter(s => !s.IsDeleted);

            builder.HasIndex(s => new { s.ParkingLotId, s.Status })
                .HasDatabaseName("IX_ParkingSpots_LotId_Status");
        }
    }
}
