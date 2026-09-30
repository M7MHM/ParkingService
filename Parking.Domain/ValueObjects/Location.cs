namespace ParkingBooking.Domain.ValueObjects;

public sealed class Location : IEquatable<Location>
{
    public double Latitude { get; }
    public double Longitude { get; }
    public string? Address { get; }

    public Location(double latitude, double longitude, string? address = null)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");

        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");

        Latitude = latitude;
        Longitude = longitude;
        Address = address;
    }

    public override string ToString() => $"{Latitude}, {Longitude}";

    public bool Equals(Location? other)
    {
        if (other is null) return false;
        return Latitude == other.Latitude && Longitude == other.Longitude;
    }

    public override bool Equals(object? obj) => Equals(obj as Location);
    public override int GetHashCode() => HashCode.Combine(Latitude, Longitude);

    public static bool operator ==(Location? left, Location? right) => Equals(left, right);
    public static bool operator !=(Location? left, Location? right) => !Equals(left, right);
}
