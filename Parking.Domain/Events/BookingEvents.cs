namespace ParkingBooking.Domain.Events;

/// <summary>
/// Event بيتم رفعه لما حجز جديد يتعمل (Pending).
/// مين هيسمعه؟ محتمل NotificationService (يبعت تأكيد أولي)، أو حاجة
/// تبدأ عداد "لو مأكدش خلال X دقيقة يبقى Expired".
/// </summary>
public class BookingCreatedEvent
{
    public Guid BookingId { get; }
    public Guid ParkingSpotId { get; }
    public Guid UserId { get; }
    public DateTime OccurredOn { get; }

    public BookingCreatedEvent(Guid bookingId, Guid parkingSpotId, Guid userId)
    {
        BookingId = bookingId;
        ParkingSpotId = parkingSpotId;
        UserId = userId;
        OccurredOn = DateTime.UtcNow;
    }
}

/// <summary>
/// Event بيتم رفعه لما حجز يتأكد (بعد الدفع مثلاً).
/// </summary>
public class BookingConfirmedEvent
{
    public Guid BookingId { get; }
    public Guid ParkingSpotId { get; }
    public DateTime OccurredOn { get; }

    public BookingConfirmedEvent(Guid bookingId, Guid parkingSpotId)
    {
        BookingId = bookingId;
        ParkingSpotId = parkingSpotId;
        OccurredOn = DateTime.UtcNow;
    }
}

/// <summary>
/// Event بيتم رفعه لما حجز يتلغي. مين هيسمعه؟ الجزء اللي مسؤول عن
/// تفريغ الموقف (parkingLot.ReleaseSpot) لازم يستمع للـ event ده.
/// </summary>
public class BookingCancelledEvent
{
    public Guid BookingId { get; }
    public Guid ParkingSpotId { get; }
    public DateTime OccurredOn { get; }

    public BookingCancelledEvent(Guid bookingId, Guid parkingSpotId)
    {
        BookingId = bookingId;
        ParkingSpotId = parkingSpotId;
        OccurredOn = DateTime.UtcNow;
    }
}

/// <summary>
/// Event بيتم رفعه لما الحجز يخلص عادي (السيارة خرجت).
/// نفس فكرة BookingCancelledEvent — الموقف لازم يتفضّى.
/// </summary>
public class BookingCompletedEvent
{
    public Guid BookingId { get; }
    public Guid ParkingSpotId { get; }
    public DateTime OccurredOn { get; }

    public BookingCompletedEvent(Guid bookingId, Guid parkingSpotId)
    {
        BookingId = bookingId;
        ParkingSpotId = parkingSpotId;
        OccurredOn = DateTime.UtcNow;
    }
}
