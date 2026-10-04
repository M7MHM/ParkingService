namespace ParkingBooking.Domain.Events;
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
