using ParkingBooking.Domain.Enums;

namespace ParkingBooking.Domain.Exceptions;

public class InvalidBookingStatusTransitionException : DomainException
{
    public Guid BookingId { get; }
    public BookingStatus CurrentStatus { get; }
    public BookingStatus AttemptedStatus { get; }

    public InvalidBookingStatusTransitionException(Guid bookingId, BookingStatus currentStatus, BookingStatus attemptedStatus)
        : base($"الحجز رقم {bookingId} حالته حاليًا {currentStatus}، مينفعش يتحول لـ {attemptedStatus}.")
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
        : base($"الحجز رقم {bookingId} مينفعش يتلغي بعد ما فترته بدأت.")
    {
        BookingId = bookingId;
    }
}
