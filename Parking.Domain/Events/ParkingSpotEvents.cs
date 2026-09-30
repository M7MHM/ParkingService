namespace ParkingBooking.Domain.Events;

public class ParkingSpotReservedEvent
{
    public Guid SpotId { get; }
    public Guid BookingId { get; }
    public DateTime ReservedUntil { get; }
    public DateTime OccurredOn { get; }

    public ParkingSpotReservedEvent(Guid spotId, Guid bookingId, DateTime reservedUntil)
    {
        SpotId = spotId;
        BookingId = bookingId;
        ReservedUntil = reservedUntil;
        OccurredOn = DateTime.UtcNow;
    }
}

public class ParkingSpotReleasedEvent
{
    public Guid SpotId { get; }
    public Guid? PreviousBookingId { get; }
    public DateTime OccurredOn { get; }

    public ParkingSpotReleasedEvent(Guid spotId, Guid? previousBookingId)
    {
        SpotId = spotId;
        PreviousBookingId = previousBookingId;
        OccurredOn = DateTime.UtcNow;
    }
}
