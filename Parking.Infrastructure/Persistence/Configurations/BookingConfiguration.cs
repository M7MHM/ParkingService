using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Infrastructure.Persistence.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("Bookings");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.ParkingSpotId);
            builder.Property(b => b.UserId);

            builder.OwnsOne(b => b.Period, period =>
            {
                period.Property(p => p.StartTime).HasColumnName("StartTime");
                period.Property(p => p.EndTime).HasColumnName("EndTime");
            });

            builder.OwnsOne(b => b.TotalPrice, money =>
            {
                money.Property(m => m.Amount).HasColumnName("TotalPriceAmount").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("TotalPriceCurrency").HasMaxLength(3);
            });

            builder.Property(b => b.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(b => b.Notes).HasMaxLength(2000);
            builder.Property(b => b.CreatedAt);
            builder.Property(b => b.UpdatedAt);
            builder.Property(b => b.IsDeleted);

            builder.HasQueryFilter(b => !b.IsDeleted);

            builder.HasIndex(b => b.UserId)
                .HasDatabaseName("IX_Bookings_UserId");

            builder.HasIndex(b => b.ParkingSpotId)
                .HasDatabaseName("IX_Bookings_ParkingSpotId");
        }
    }
}
