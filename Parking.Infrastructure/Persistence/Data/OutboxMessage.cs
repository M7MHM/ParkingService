using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Infrastructure.Persistence.Data
{
    public sealed class OutboxMessage
    {
        public Guid Id { get; set; }
        public string RoutingKey { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime OccurredAtUtc { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
        public int Attempts { get; set; }
        public string? LastError { get; set; }
    }

    public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages");
            builder.HasKey(m => m.Id);

            builder.Property(m => m.RoutingKey).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Payload).IsRequired();
            builder.Property(m => m.LastError).HasMaxLength(500);

            builder.HasIndex(m => new { m.ProcessedAtUtc, m.OccurredAtUtc })
                .HasDatabaseName("IX_OutboxMessages_Pending");
        }
    }
}
