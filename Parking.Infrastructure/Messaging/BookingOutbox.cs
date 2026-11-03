using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Parking.Application.Interfaces;
using Parking.Contracts;
using Parking.Infrastructure.Persistence.Data;
using ParkingBooking.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Parking.Infrastructure.Messaging
{
    internal sealed class OutboxSnapshotException : Exception
    {
        public OutboxSnapshotException(string message) : base(message) { }
    }
    internal sealed class BookingOutboxWriter
    {
        private readonly AppDbContext _db;
        private readonly ILogger<BookingOutboxWriter> _logger;

        public BookingOutboxWriter(AppDbContext db, ILogger<BookingOutboxWriter> logger)
        {
            _db = db;
            _logger = logger;
        }
        public async Task EnqueueAsync(
            Guid eventId,
            DateTime occurredOn,
            Guid bookingId,
            string routingKey,
            CancellationToken ct)
        {
            if (eventId == Guid.Empty)
                throw new OutboxSnapshotException("A booking event must have a non-empty EventId.");

            if (_db.OutboxMessages.Local.Any(m => m.Id == eventId))
                return;

            if (await _db.OutboxMessages.AnyAsync(m => m.Id == eventId, ct))
                return;

            var booking = _db.Bookings.Local.FirstOrDefault(b => b.Id == bookingId);
            if (booking is null)
                throw new OutboxSnapshotException(
                    $"Booking {bookingId} was not tracked while creating event {eventId}.");

            var user = await _db.Users
                .IgnoreQueryFilters()
                .Where(u => u.Id == booking.UserId)
                .Select(u => new
                {
                    Email = u.Email.Value,
                    u.FullName,
                    u.IsDeleted
                })
                .SingleOrDefaultAsync(ct);

            if (user is null)
                throw new OutboxSnapshotException(
                    $"User {booking.UserId} was not found for booking {bookingId}.");

            if (user.IsDeleted)
            {
                _logger.LogInformation(
                    "Skipping notification for booking {BookingId}; recipient {UserId} is soft-deleted.",
                    bookingId, booking.UserId);
                return;
            }

            var spot = await _db.ParkingSpots
                .IgnoreQueryFilters()
                .Where(s => s.Id == booking.ParkingSpotId)
                .Select(s => new { s.SpotNumber, LotName = s.ParkingLot.Name })
                .SingleOrDefaultAsync(ct);

            if (spot is null || string.IsNullOrWhiteSpace(spot.LotName))
                throw new OutboxSnapshotException(
                    $"Parking spot or lot was not found for booking {bookingId}.");

            var occurredAtUtc = AsUtc(occurredOn);
            var message = new BookingEventMessage(
                MessageId: eventId,
                OccurredAtUtc: occurredAtUtc,
                BookingId: booking.Id,
                UserId: booking.UserId,
                RecipientEmail: user.Email,
                RecipientName: user.FullName,
                LotName: spot.LotName,
                SpotNumber: spot.SpotNumber,
                StartTimeUtc: AsUtc(booking.Period.StartTime),
                EndTimeUtc: AsUtc(booking.Period.EndTime),
                TotalAmount: booking.TotalPrice.Amount,
                Currency: booking.TotalPrice.Currency);

            _db.OutboxMessages.Add(new OutboxMessage
            {
                Id = eventId,
                RoutingKey = routingKey,
                Payload = JsonSerializer.Serialize(message, MessageJson.Options),
                OccurredAtUtc = occurredAtUtc
            });
        }
        private static DateTime AsUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => throw new OutboxSnapshotException("Unsupported DateTime kind.")
        };
    }
    internal sealed class BookingConfirmedOutboxHandler : IDomainEventHandler<BookingConfirmedEvent>
    {
        private readonly BookingOutboxWriter _writer;
        public BookingConfirmedOutboxHandler(BookingOutboxWriter writer) => _writer = writer;

        public Task HandleAsync(BookingConfirmedEvent domainEvent, CancellationToken ct) =>
            _writer.EnqueueAsync(domainEvent.EventId, domainEvent.OccurredOn,
                domainEvent.BookingId, MessageRoutes.BookingConfirmed, ct);
    }
    internal sealed class BookingCancelledOutboxHandler : IDomainEventHandler<BookingCancelledEvent>
    {
        private readonly BookingOutboxWriter _writer;
        public BookingCancelledOutboxHandler(BookingOutboxWriter writer) => _writer = writer;

        public Task HandleAsync(BookingCancelledEvent domainEvent, CancellationToken ct) =>
            _writer.EnqueueAsync(domainEvent.EventId, domainEvent.OccurredOn,
                domainEvent.BookingId, MessageRoutes.BookingCancelled, ct);
    }
    internal sealed class BookingCompletedOutboxHandler : IDomainEventHandler<BookingCompletedEvent>
    {
        private readonly BookingOutboxWriter _writer;
        public BookingCompletedOutboxHandler(BookingOutboxWriter writer) => _writer = writer;

        public Task HandleAsync(BookingCompletedEvent domainEvent, CancellationToken ct) =>
            _writer.EnqueueAsync(domainEvent.EventId, domainEvent.OccurredOn,
                domainEvent.BookingId, MessageRoutes.BookingCompleted, ct);
    }
}
