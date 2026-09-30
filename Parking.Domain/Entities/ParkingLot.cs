using ParkingBooking.Domain.Enums;
using ParkingBooking.Domain.Exceptions;
using ParkingBooking.Domain.ValueObjects;

namespace ParkingBooking.Domain.Entities;

public class ParkingLot : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Location Location { get; private set; } = null!;
    public Money HourlyRate { get; private set; } = null!;
    public int TotalSpots { get; private set; }
    public int AvailableSpotsCount { get; private set; }
    public bool IsActive { get; private set; }
    public TimeSpan OpeningTime { get; private set; }
    public TimeSpan ClosingTime { get; private set; }

    private readonly List<ParkingSpot> _spots = new();
    public IReadOnlyCollection<ParkingSpot> Spots => _spots.AsReadOnly();

    private ParkingLot() { } 

    public ParkingLot(
        string name,
        string description,
        Location location,
        Money hourlyRate,
        int totalSpots,
        TimeSpan openingTime,
        TimeSpan closingTime)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Parking lot name is required", nameof(name));

        if (totalSpots <= 0)
            throw new ArgumentException("Total spots must be greater than zero.", nameof(totalSpots));

        if (hourlyRate.Amount < 0)
            throw new ArgumentException("Hourly rate cannot be negative.", nameof(hourlyRate));

        Name = name;
        Description = description;
        Location = location;
        HourlyRate = hourlyRate;
        TotalSpots = totalSpots;
        AvailableSpotsCount = 0;
        IsActive = true;
        OpeningTime = openingTime;
        ClosingTime = closingTime;
    }

    public void UpdateDetails(string name, string description, Money hourlyRate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Parking lot name is required", nameof(name));

        Name = name;
        Description = description;
        HourlyRate = hourlyRate;
        UpdateTimestamp();
    }

    public void AddSpot(ParkingSpot spot)
    {
        if (_spots.Count >= TotalSpots)
            throw new InvalidOperationException("Maximum number of spots reached.");

        _spots.Add(spot);

        if (spot.IsCurrentlyAvailable())
            AvailableSpotsCount++;

        UpdateTimestamp();
    }

    public void RemoveSpot(Guid spotId)
    {
        var spot = GetSpotOrThrow(spotId);

        if (spot.Status != SpotStatus.Available)
            throw new InvalidOperationException("Cannot remove a reserved or occupied spot.");

        _spots.Remove(spot);
        AvailableSpotsCount--;
        UpdateTimestamp();
    }

    public void ReserveSpot(Guid spotId, Guid bookingId, DateTime reservedUntil)
    {
        var spot = GetSpotOrThrow(spotId);

        spot.Reserve(bookingId, reservedUntil); 

        AvailableSpotsCount--;
        UpdateTimestamp();
    }


    public void ReleaseSpot(Guid spotId)
    {
        var spot = GetSpotOrThrow(spotId);

        spot.Release(); 

        AvailableSpotsCount++;
        UpdateTimestamp();
    }

    public void MarkSpotOccupied(Guid spotId)
    {
        var spot = GetSpotOrThrow(spotId);
        spot.MarkAsOccupied();
        UpdateTimestamp();
    }

    public void SetSpotMaintenance(Guid spotId)
    {
        var spot = GetSpotOrThrow(spotId);
        spot.SetMaintenance();
        UpdateTimestamp();
    }

    public void CompleteSpotMaintenance(Guid spotId)
    {
        var spot = GetSpotOrThrow(spotId);
        spot.CompleteMaintenance();
        UpdateTimestamp();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public bool IsOpenAt(DateTime dateTime)
    {
        var timeOfDay = dateTime.TimeOfDay;

        return IsActive && timeOfDay >= OpeningTime && timeOfDay <= ClosingTime;
    }

    private ParkingSpot GetSpotOrThrow(Guid spotId)
    {
        var spot = _spots.FirstOrDefault(s => s.Id == spotId);
        if (spot is null)
            throw new SpotNotFoundException(spotId);

        return spot;
    }
}
