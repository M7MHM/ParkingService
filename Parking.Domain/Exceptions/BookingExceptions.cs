using ParkingBooking.Domain.Enums;

namespace ParkingBooking.Domain.Exceptions;

/// <summary>
/// بيتم رميه لما حد يحاول يعمل status transition مش مسموح بيه
/// (مثلاً يلغي حجز already Completed)
/// </summary>
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

/// <summary>
/// بيتم رميه لما حد يحاول يلغي حجز بعد ما فترته بدأت فعلاً
/// (قرار بيزنس: لغاء بعد بداية الفترة مش منطقي — ممكن تغيّره حسب الـ policy بتاعتك)
/// </summary>
public class BookingCannotBeCancelledException : DomainException
{
    public Guid BookingId { get; }

    public BookingCannotBeCancelledException(Guid bookingId)
        : base($"الحجز رقم {bookingId} مينفعش يتلغي بعد ما فترته بدأت.")
    {
        BookingId = bookingId;
    }
}
