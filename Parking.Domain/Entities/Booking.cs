using ParkingBooking.Domain.Entities;
using ParkingBooking.Domain.Enums;
using ParkingBooking.Domain.Events;
using ParkingBooking.Domain.Exceptions;
using ParkingBooking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Domain.Entities;

public class Booking : BaseEntity
{
    public Guid ParkingSpotId { get; private set; }
    public Guid UserId { get; private set; }
    public BookingPeriod Period { get; private set; } = null!;
    public Money TotalPrice { get; private set; } = null!;
    public BookingStatus Status { get; private set; }
    public string? Notes { get; private set; }

    private Booking() { } 

    public Booking(
        Guid parkingSpotId,
        Guid userId,
        BookingPeriod period,
        Money totalPrice,
        string? notes = null)
    {
        ParkingSpotId = parkingSpotId;
        UserId = userId;
        Period = period ?? throw new ArgumentNullException(nameof(period));
        TotalPrice = totalPrice ?? throw new ArgumentNullException(nameof(totalPrice));
        Notes = notes;
        Status = BookingStatus.Pending;

        AddDomainEvent(new BookingCreatedEvent(Id, ParkingSpotId, UserId));
    }
    public void Confirm()
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidBookingStatusTransitionException(Id, Status, BookingStatus.Confirmed);

        Status = BookingStatus.Confirmed;

        AddDomainEvent(new BookingConfirmedEvent(Id, ParkingSpotId));

        UpdateTimestamp();
    }
    public void Cancel()
    {
        if (Status is not (BookingStatus.Pending or BookingStatus.Confirmed))
            throw new InvalidBookingStatusTransitionException(Id, Status, BookingStatus.Cancelled);

        if (Period.HasStarted(DateTime.UtcNow))
            throw new BookingCannotBeCancelledException(Id);

        var wasConfirmed = Status == BookingStatus.Confirmed;

        Status = BookingStatus.Cancelled;

        if (wasConfirmed)
            AddDomainEvent(new BookingCancelledEvent(Id, ParkingSpotId));

        UpdateTimestamp();
    }
    public void Complete()
    {
        if (Status != BookingStatus.Confirmed)
            throw new InvalidBookingStatusTransitionException(Id, Status, BookingStatus.Completed);

        Status = BookingStatus.Completed;

        AddDomainEvent(new BookingCompletedEvent(Id, ParkingSpotId));

        UpdateTimestamp();
    }
    public void MarkExpired()
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidBookingStatusTransitionException(Id, Status, BookingStatus.Expired);

        Status = BookingStatus.Expired;
        UpdateTimestamp();
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdateTimestamp();
    }
}
