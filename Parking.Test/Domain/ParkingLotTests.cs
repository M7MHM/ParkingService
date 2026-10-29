using FluentAssertions;
using Parking.Test.Common;
using ParkingBooking.Domain.Entities;
using ParkingBooking.Domain.Enums;
using ParkingBooking.Domain.Events;
using ParkingBooking.Domain.Exceptions;
using ParkingBooking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Test.Domain
{
    public sealed class ParkingLotTests
    {
        [Fact]
        public void IsOpenAt_WithinConfiguredHoursAndActive_ReturnsTrue()
        {
            var lot = TestData.Lot(8, 18);

            lot.IsOpenAt(new DateTime(2030, 5, 10, 12, 30, 0)).Should().BeTrue();
        }
        [Theory]
        [InlineData(7, 59)]
        [InlineData(18, 1)]
        public void IsOpenAt_OutsideConfiguredHours_ReturnsFalse(int hour, int minute)
        {
            var lot = TestData.Lot(8, 18);

            lot.IsOpenAt(new DateTime(2030, 5, 10, hour, minute, 0)).Should().BeFalse();
        }

        [Fact]
        public void IsOpenAt_AtClosingTime_ReturnsTrue()
        {
            var lot = TestData.Lot(8, 18);

            lot.IsOpenAt(new DateTime(2030, 5, 10, 18, 0, 0)).Should().BeTrue();
        }

        [Fact]
        public void IsOpenAt_WhenLotIsInactive_ReturnsFalse()
        {
            var lot = TestData.Lot(8, 18);
            lot.Deactivate();

            lot.IsOpenAt(new DateTime(2030, 5, 10, 12, 30, 0)).Should().BeFalse();
        }
        [Fact]
        public void IsOpenAt_FullDayLot_IsOpenUntilTheLastSecondOfTheDay()
        {
            var lot = TestData.Lot(0, 24);

            lot.IsOpenAt(new DateTime(2030, 5, 10, 0, 0, 0)).Should().BeTrue();
            lot.IsOpenAt(new DateTime(2030, 5, 10, 23, 59, 59)).Should().BeTrue();
        }
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_BlankName_ThrowsArgumentException(string name)
        {
            Action act = () => new ParkingLot(
                name, "d", new Location(30, 31), new Money(10m), 5, TimeSpan.Zero, TimeSpan.FromHours(24));

            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public void Constructor_NonPositiveTotalSpots_ThrowsArgumentException(int totalSpots)
        {
            Action act = () => new ParkingLot(
                "Lot", "d", new Location(30, 31), new Money(10m), totalSpots, TimeSpan.Zero, TimeSpan.FromHours(24));

            act.Should().Throw<ArgumentException>();
        }
        [Theory]
        [InlineData(8, 8)]     
        [InlineData(18, 8)]   
        [InlineData(0, 25)]    
        [InlineData(-1, 5)]   
        [InlineData(24, 25)]  
        public void Constructor_InvalidOperatingHours_ThrowsArgumentException(int openHour, int closeHour)
        {
            Action act = () => new ParkingLot(
                "Lot", "d", new Location(30, 31), new Money(10m), 5,
                TimeSpan.FromHours(openHour), TimeSpan.FromHours(closeHour));

            act.Should().Throw<ArgumentException>(); 
        }
        [Fact]
        public void Constructor_OvernightHours_AreRejectedByDesign()
        {
            Action act = () => new ParkingLot(
                "Night Lot", "d", new Location(30, 31), new Money(10m), 5,
                TimeSpan.FromHours(22), TimeSpan.FromHours(6));

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_FullDayHours_IsAccepted()
        {
            Action act = () => TestData.Lot(0, 24);

            act.Should().NotThrow();
        }

        [Fact]
        public void Constructor_ValidArguments_StartsActiveWithNoAvailableSpots()
        {
            var lot = TestData.Lot(8, 18);

            lot.IsActive.Should().BeTrue();
            lot.AvailableSpotsCount.Should().Be(0);
            lot.Spots.Should().BeEmpty();
        }

        [Fact]
        public void AddSpot_AvailableSpot_IncrementsAvailableCount()
        {
            var lot = TestData.LotWithSpot(out _);

            lot.AvailableSpotsCount.Should().Be(1);
        }

        [Fact]
        public void AddSpot_BeyondTotalSpots_ThrowsAndKeepsCount()
        {
            var lot = TestData.Lot(totalSpots: 1);
            lot.AddSpot(TestData.Spot(lot, "A-01"));

            Action act = () => lot.AddSpot(TestData.Spot(lot, "A-02"));

            act.Should().Throw<InvalidOperationException>();
            lot.Spots.Should().HaveCount(1);
            lot.AvailableSpotsCount.Should().Be(1);
        }

        [Fact]
        public void RemoveSpot_AvailableSpot_RemovesItAndDecrementsCount()
        {
            var lot = TestData.LotWithSpot(out var spot);

            lot.RemoveSpot(spot.Id);

            lot.Spots.Should().BeEmpty();
            lot.AvailableSpotsCount.Should().Be(0);
        }

        [Fact]
        public void RemoveSpot_ReservedSpot_ThrowsAndKeepsSpot()
        {
            var lot = TestData.LotWithSpot(out var spot);
            lot.ReserveSpot(spot.Id, Guid.NewGuid(), DateTime.UtcNow.AddHours(2));

            Action act = () => lot.RemoveSpot(spot.Id);

            act.Should().Throw<InvalidOperationException>();
            lot.Spots.Should().HaveCount(1);
        }

        [Fact]
        public void RemoveSpot_UnknownSpot_ThrowsSpotNotFound()
        {
            var lot = TestData.Lot();

            Action act = () => lot.RemoveSpot(Guid.NewGuid());

            act.Should().Throw<SpotNotFoundException>();
        }
        [Fact]
        public void ReserveSpot_AvailableSpot_ReservesDecrementsCountAndRaisesEvent()
        {
            var lot = TestData.LotWithSpot(out var spot);
            var bookingId = Guid.NewGuid();
            var until = DateTime.UtcNow.AddHours(2);

            lot.ReserveSpot(spot.Id, bookingId, until);

            spot.Status.Should().Be(SpotStatus.Reserved);
            spot.CurrentBookingId.Should().Be(bookingId);
            spot.ReservedUntil.Should().Be(until);
            lot.AvailableSpotsCount.Should().Be(0);
            spot.DomainEvents.OfType<ParkingSpotReservedEvent>().Should().ContainSingle()
                .Which.BookingId.Should().Be(bookingId);
        }
        [Fact]
        public void ReserveSpot_AlreadyReserved_ThrowsAndDoesNotDecrementTwice()
        {
            var lot = TestData.LotWithSpot(out var spot);
            lot.ReserveSpot(spot.Id, Guid.NewGuid(), DateTime.UtcNow.AddHours(2));

            Action act = () => lot.ReserveSpot(spot.Id, Guid.NewGuid(), DateTime.UtcNow.AddHours(3));

            act.Should().Throw<SpotNotAvailableException>();
            lot.AvailableSpotsCount.Should().Be(0);
        }
        [Fact]
        public void ReserveSpot_ReservationEndInThePast_ThrowsAndKeepsSpotAvailable()
        {
            var lot = TestData.LotWithSpot(out var spot);

            Action act = () => lot.ReserveSpot(spot.Id, Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-1));

            act.Should().Throw<ArgumentException>();
            spot.Status.Should().Be(SpotStatus.Available);
            lot.AvailableSpotsCount.Should().Be(1);
        }
        [Fact]
        public void ReserveSpot_UnknownSpot_ThrowsSpotNotFound()
        {
            var lot = TestData.Lot();

            Action act = () => lot.ReserveSpot(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddHours(1));

            act.Should().Throw<SpotNotFoundException>();
        }
        [Fact]
        public void ReleaseSpot_ReservedSpot_FreesItIncrementsCountAndRaisesEvent()
        {
            var lot = TestData.LotWithSpot(out var spot);
            var bookingId = Guid.NewGuid();
            lot.ReserveSpot(spot.Id, bookingId, DateTime.UtcNow.AddHours(2));

            lot.ReleaseSpot(spot.Id);

            spot.Status.Should().Be(SpotStatus.Available);
            spot.CurrentBookingId.Should().BeNull();
            spot.ReservedUntil.Should().BeNull();
            lot.AvailableSpotsCount.Should().Be(1);
            spot.DomainEvents.OfType<ParkingSpotReleasedEvent>().Should().ContainSingle()
                .Which.PreviousBookingId.Should().Be(bookingId);
        }
        [Fact]
        public void ReleaseSpot_AlreadyAvailable_ThrowsAndDoesNotInflateCount()
        {
            var lot = TestData.LotWithSpot(out var spot);

            Action act = () => lot.ReleaseSpot(spot.Id);

            act.Should().Throw<InvalidOperationException>();
            lot.AvailableSpotsCount.Should().Be(1);
        }
        [Fact]
        public void MarkSpotOccupied_ReservedSpot_MovesToOccupiedWithoutChangingCount()
        {
            var lot = TestData.LotWithSpot(out var spot);
            lot.ReserveSpot(spot.Id, Guid.NewGuid(), DateTime.UtcNow.AddHours(2));

            lot.MarkSpotOccupied(spot.Id);

            spot.Status.Should().Be(SpotStatus.Occupied);
            lot.AvailableSpotsCount.Should().Be(0);
        }
        [Fact]
        public void MarkSpotOccupied_AvailableSpot_Throws()
        {
            var lot = TestData.LotWithSpot(out var spot);

            Action act = () => lot.MarkSpotOccupied(spot.Id);

            act.Should().Throw<InvalidOperationException>();
        }
        [Fact]
        public void SetSpotMaintenance_AvailableSpot_DecrementsCount()
        {
            var lot = TestData.LotWithSpot(out var spot);

            lot.SetSpotMaintenance(spot.Id);

            spot.Status.Should().Be(SpotStatus.Maintenance);
            lot.AvailableSpotsCount.Should().Be(0);
        }
        [Fact]
        public void SetSpotMaintenance_CalledTwice_DoesNotDecrementTwice()
        {
            var lot = TestData.LotWithSpot(out var spot);
            lot.SetSpotMaintenance(spot.Id);

            lot.SetSpotMaintenance(spot.Id);

            lot.AvailableSpotsCount.Should().Be(0);
        }
        [Fact]
        public void CompleteSpotMaintenance_SpotUnderMaintenance_IncrementsCount()
        {
            var lot = TestData.LotWithSpot(out var spot);
            lot.SetSpotMaintenance(spot.Id);

            lot.CompleteSpotMaintenance(spot.Id);

            spot.Status.Should().Be(SpotStatus.Available);
            lot.AvailableSpotsCount.Should().Be(1);
        }
        [Fact]
        public void SetSpotMaintenance_ReservedSpot_ThrowsAndKeepsReservation()
        {
            var lot = TestData.LotWithSpot(out var spot);
            lot.ReserveSpot(spot.Id, Guid.NewGuid(), DateTime.UtcNow.AddHours(2));

            Action act = () => lot.SetSpotMaintenance(spot.Id);

            act.Should().Throw<InvalidOperationException>();
            spot.Status.Should().Be(SpotStatus.Reserved);
            lot.AvailableSpotsCount.Should().Be(0);
        }
    }
}