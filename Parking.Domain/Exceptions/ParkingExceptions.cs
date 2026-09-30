namespace ParkingBooking.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception inner) : base(message, inner) { }
}

public class SpotNotAvailableException : DomainException
{
    public Guid SpotId { get; }

    public SpotNotAvailableException(Guid spotId)
        : base($"الموقف رقم {spotId} مش متاح للحجز حالياً.")
    {
        SpotId = spotId;
    }
}

public class SpotNotFoundException : DomainException
{
    public Guid SpotId { get; }

    public SpotNotFoundException(Guid spotId)
        : base($"الموقف رقم {spotId} مش موجود في الـ Parking Lot ده.")
    {
        SpotId = spotId;
    }
}
