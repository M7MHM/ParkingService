using ParkingBooking.Domain.Enums;

namespace ParkingBooking.Domain.Exceptions;

public class InvalidBookingStatusTransitionException : DomainException
{
    public Guid BookingId { get; }
    public BookingStatus CurrentStatus { get; }
    public BookingStatus AttemptedStatus { get; }

    public InvalidBookingStatusTransitionException(Guid bookingId, BookingStatus currentStatus, BookingStatus attemptedStatus)
        : base($"Booking {bookingId} is currently in {currentStatus} status and cannot be transitioned to {attemptedStatus}.")
    {
        BookingId = bookingId;
        CurrentStatus = currentStatus;
        AttemptedStatus = attemptedStatus;
    }
}

public class BookingCannotBeCancelledException : DomainException
{
    public Guid BookingId { get; }

    public BookingCannotBeCancelledException(Guid bookingId)
        : base($"Booking {bookingId} cannot be cancelled after its period has started.")
    {
        BookingId = bookingId;
    }
}
