namespace ParkingBooking.Domain.ValueObjects;

public sealed class SpotType : IEquatable<SpotType>
{
    public Enums.SpotSize Size { get; }
    public bool HasElectricCharger { get; }
    public bool IsCovered { get; }
    public bool IsAccessible { get; }

    public SpotType(
        Enums.SpotSize size,
        bool hasElectricCharger = false,
        bool isCovered = false,
        bool isAccessible = false)
    {
        Size = size;
        HasElectricCharger = hasElectricCharger;
        IsCovered = isCovered;
        IsAccessible = isAccessible;
    }

    public static SpotType Standard() => new SpotType(Enums.SpotSize.Standard);
    public static SpotType VIP() => new SpotType(Enums.SpotSize.VIP, isCovered: true);
    public static SpotType Electric() => new SpotType(Enums.SpotSize.Electric, hasElectricCharger: true);
    public static SpotType Accessible() => new SpotType(Enums.SpotSize.Accessible, isAccessible: true);

    public bool Equals(SpotType? other)
    {
        if (other is null) return false;
        return Size == other.Size &&
               HasElectricCharger == other.HasElectricCharger &&
               IsCovered == other.IsCovered &&
               IsAccessible == other.IsAccessible;
    }

    public override bool Equals(object? obj) => Equals(obj as SpotType);
    public override int GetHashCode() => HashCode.Combine(Size, HasElectricCharger, IsCovered, IsAccessible);

    public static bool operator ==(SpotType? left, SpotType? right) => Equals(left, right);
    public static bool operator !=(SpotType? left, SpotType? right) => !Equals(left, right);
}
