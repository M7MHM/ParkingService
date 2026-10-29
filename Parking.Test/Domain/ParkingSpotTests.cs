using FluentAssertions;
using ParkingBooking.Domain.Entities;
using ParkingBooking.Domain.Enums;
using ParkingBooking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Test.Domain
{
    public sealed class ParkingSpotTests
    {
        [Fact]
        public void Constructor_BlankSpotNumber_ThrowsArgumentException()
        {
            Action act = () => new ParkingSpot(" ", SpotType.Standard(), new Money(10m), Guid.NewGuid());

            act.Should().Throw<ArgumentException>();
        }
        [Fact]
        public void Constructor_ValidArguments_StartsAvailable()
        {
            var spot = new ParkingSpot("A-01", SpotType.Standard(), new Money(10m), Guid.NewGuid());

            spot.Status.Should().Be(SpotStatus.Available);
            spot.IsCurrentlyAvailable().Should().BeTrue();
            spot.CurrentBookingId.Should().BeNull();
        }
        [Theory]
        [InlineData(1, 25)]
        [InlineData(3, 75)]
        public void CalculatePrice_MultipliesHourlyRateByHours(int hours, int expectedAmount)
        {
            var spot = new ParkingSpot("A-01", SpotType.Standard(), new Money(25m), Guid.NewGuid());

            spot.CalculatePrice(hours).Amount.Should().Be(expectedAmount);
        }
        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        public void CalculatePrice_NonPositiveHours_ThrowsArgumentException(int hours)
        {
            var spot = new ParkingSpot("A-01", SpotType.Standard(), new Money(25m), Guid.NewGuid());

            Action act = () => spot.CalculatePrice(hours);

            act.Should().Throw<ArgumentException>();
        }
    }
}
