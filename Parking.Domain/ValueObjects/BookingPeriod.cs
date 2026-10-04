namespace ParkingBooking.Domain.ValueObjects;

public sealed class BookingPeriod : IEquatable<BookingPeriod>
{
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }

    public TimeSpan Duration => EndTime - StartTime;

    public BookingPeriod(DateTime startTime, DateTime endTime)
    {
        if (endTime <= startTime)
            throw new ArgumentException("وقت النهاية لازم يكون بعد وقت البداية", nameof(endTime));

        StartTime = startTime;
        EndTime = endTime;
    }
    public int WholeHoursCeiling() => (int)Math.Ceiling(Duration.TotalHours);
    public bool Overlaps(BookingPeriod other)
    {
        return StartTime < other.EndTime && other.StartTime < EndTime;
    }

    public bool HasStarted(DateTime asOf) => asOf >= StartTime;

    public override string ToString() => $"{StartTime:yyyy-MM-dd HH:mm} → {EndTime:yyyy-MM-dd HH:mm}";

    public bool Equals(BookingPeriod? other)
    {
        if (other is null) return false;
        return StartTime == other.StartTime && EndTime == other.EndTime;
    }

    public override bool Equals(object? obj) => Equals(obj as BookingPeriod);
    public override int GetHashCode() => HashCode.Combine(StartTime, EndTime);

    public static bool operator ==(BookingPeriod? left, BookingPeriod? right) => Equals(left, right);
    public static bool operator !=(BookingPeriod? left, BookingPeriod? right) => !Equals(left, right);
}
