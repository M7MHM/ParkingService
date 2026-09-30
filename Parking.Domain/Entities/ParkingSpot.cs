using ParkingBooking.Domain.Enums;
using ParkingBooking.Domain.Events;
using ParkingBooking.Domain.Exceptions;
using ParkingBooking.Domain.ValueObjects;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ParkingBooking.Domain.Entities;

public class ParkingSpot : BaseEntity
{
    public string SpotNumber { get; private set; } = string.Empty;
    public SpotType Type { get; private set; } = null!;
    public SpotStatus Status { get; private set; }
    public Money HourlyRate { get; private set; } = null!;
    public Guid ParkingLotId { get; private set; }
    public ParkingLot ParkingLot { get; private set; } = null!;

    public Guid? CurrentBookingId { get; private set; }
    public DateTime? ReservedUntil { get; private set; }

    private ParkingSpot() { } 

    public ParkingSpot(
        string spotNumber,
        SpotType type,
        Money hourlyRate,
        Guid parkingLotId)
    {
        if (string.IsNullOrWhiteSpace(spotNumber))
            throw new ArgumentException("Spot number is required.", nameof(spotNumber));

        if (hourlyRate.Amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(hourlyRate));

        SpotNumber = spotNumber;
        Type = type;
        HourlyRate = hourlyRate;
        Status = SpotStatus.Available;
        ParkingLotId = parkingLotId;
    }
    internal void Reserve(Guid bookingId, DateTime reservedUntil)
    {
        if (Status != SpotStatus.Available)
            throw new SpotNotAvailableException(Id);

        if (reservedUntil <= DateTime.UtcNow)
            throw new ArgumentException("Reservation end date must be in the future.", nameof(reservedUntil));

        Status = SpotStatus.Reserved;
        CurrentBookingId = bookingId;
        ReservedUntil = reservedUntil;

        AddDomainEvent(new ParkingSpotReservedEvent(Id, bookingId, reservedUntil));

        UpdateTimestamp();
    }
    internal void MarkAsOccupied()
    {
        if (Status != SpotStatus.Reserved)
            throw new InvalidOperationException("Cannot mark spot as occupied unless it is reserved.");

        Status = SpotStatus.Occupied;
        UpdateTimestamp();
    }
    internal void Release()
    {
        if (Status == SpotStatus.Available)
            throw new InvalidOperationException("Spot is already available.");

        var previousBookingId = CurrentBookingId;

        Status = SpotStatus.Available;
        CurrentBookingId = null;
        ReservedUntil = null;

        AddDomainEvent(new ParkingSpotReleasedEvent(Id, previousBookingId));

        UpdateTimestamp();
    }
    internal void SetMaintenance()
    {
        if (Status is SpotStatus.Occupied or SpotStatus.Reserved)
            throw new InvalidOperationException("Cannot put a reserved or occupied spot under maintenance.");

        Status = SpotStatus.Maintenance;
        UpdateTimestamp();
    }

    internal void CompleteMaintenance()
    {
        if (Status != SpotStatus.Maintenance)
            throw new InvalidOperationException("Number of hours must be greater than zero.");

        Status = SpotStatus.Available;
        UpdateTimestamp();
    }

    public Money CalculatePrice(int hours)
    {
        if (hours <= 0)
            throw new ArgumentException("Total Hours must be greater than zero.", nameof(hours));

        return HourlyRate.Multiply(hours);
    }

    public bool IsCurrentlyAvailable() => Status == SpotStatus.Available;

    public bool IsReservationExpired()
    {
        return Status == SpotStatus.Reserved &&
               ReservedUntil.HasValue &&
               ReservedUntil.Value <= DateTime.UtcNow;
    }
    public void UpdateType(SpotType newType)
    {
        Type = newType;
        UpdateTimestamp();
    }

    public void UpdateRate(Money newRate)
    {
        if (newRate.Amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(newRate));

        HourlyRate = newRate;
        UpdateTimestamp();
    }
}
